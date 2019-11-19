using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace RabbitMQ.MessageBus
{
  public class RabbitMQMessageBus : IMessageBus, IDisposable
  {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly IRabbitMQPersistentConnection _persistentConnection;

    private readonly string _queueName;
    private readonly string _brokerName;
    private readonly string _brokerStrategy;
    private readonly int _retryCount;
    private readonly bool _confirmSelect;

    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;

    private IModel _consumerChannel;

    public RabbitMQMessageBus(
      ILogger<RabbitMQMessageBus> logger,
      IServiceProvider serviceProvider,
      IRabbitMQPersistentConnection persistentConnection,
      IOptions<RabbitMQMessageBusConfiguration> rabbitMQMessageBusConfiguration
    )
    {
      _logger = logger;
      _serviceProvider = serviceProvider;
      _persistentConnection = persistentConnection;

      var config = rabbitMQMessageBusConfiguration.Value;
      _queueName = config.QueueName;
      _brokerName = config.BrokerName;
      _brokerStrategy = config.BrokerStrategy;
      _retryCount = config.RetryCount;
      _confirmSelect = config.ConfirmSelect;

      _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
      _consumerChannel = CreateConsumerChannel();
    }

    public async Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)) where TMessage : class
    {
      if (!_persistentConnection.IsConnected)
      {
        _persistentConnection.TryConnect();
      }

      var policy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(
                  _retryCount,
                  retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                  {
                    _logger.LogWarning(ex.ToString());
                  }
                );

      using (var channel = _persistentConnection.CreateModel())
      {
        var messageType = message.GetType().Name;

        channel.ExchangeDeclare(exchange: _brokerName, type: _brokerStrategy);

        var rawMessage = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(rawMessage);

        if (_confirmSelect)
        {
          channel.ConfirmSelect();
          channel.BasicAcks += (sender, ea) =>
          {
            // code when message is confirmed
            _logger.LogTrace("Ack for message {Message}", rawMessage);
            Console.WriteLine($"Ack for message {rawMessage}");
          };
          channel.BasicNacks += (sender, ea) =>
          {
            //code when message is nack-ed
            _logger.LogTrace("Nack for message {Message}", rawMessage);
            Console.WriteLine($"Nack for message {rawMessage}");
          };
        }

        policy.Execute(() =>
        {
          var properties = channel.CreateBasicProperties();
          properties.DeliveryMode = 2; // persistent

          channel.BasicPublish(
            exchange: this._brokerName,
            routingKey: messageType,
            basicProperties: properties,
            body: body);
        });
      }

      await Task.CompletedTask;
    }

    public Guid Subscribe<THandler, TMessage>()
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(typeof(THandler), typeof(TMessage));
      DoInternalSubscription(subscription.MessageTypeName);
      _subscriptions.TryAdd(subscription.Id, subscription);

      return subscription.Id;
    }

    public bool Unsubscribe(Guid subscriptionId)
    {
      return _subscriptions.TryRemove(subscriptionId, out Subscription sub);
    }

    public void Dispose()
    {
      if (_consumerChannel != null)
      {
        _consumerChannel.Dispose();
      }

      _subscriptions.Clear();
    }

    private IModel CreateConsumerChannel()
    {
      if (!_persistentConnection.IsConnected)
      {
        _persistentConnection.TryConnect();
      }

      var channel = _persistentConnection.CreateModel();

      channel.ExchangeDeclare(exchange: _brokerName, type: _brokerStrategy);

      channel.QueueDeclare(queue: _queueName,
                           durable: true,
                           exclusive: false,
                           autoDelete: false,
                           arguments: null);

      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += async (model, ea) =>
      {
        var eventName = ea.RoutingKey;
        var message = Encoding.UTF8.GetString(ea.Body);

        await ProcessEvent(eventName, message);
      };

      channel.BasicConsume(queue: _queueName,
                           autoAck: true,
                           consumer: consumer);

      channel.CallbackException += (sender, ea) =>
      {
        _consumerChannel.Dispose();
        _consumerChannel = CreateConsumerChannel();
      };

      return channel;
    }

    private async Task ProcessEvent(string messageType, string message)
    {
      if (HasSubscriptionsForEvent(messageType))
      {
        var subscriptionsForMessageType = _subscriptions
          .Values
          .Where(x => x.MessageTypeName == messageType);
        foreach (var subscription in subscriptionsForMessageType)
        {
          try
          {
            var messageToHandle = JsonConvert.DeserializeObject(message, subscription.MessageType);
            await subscription.Handle(_serviceProvider, messageToHandle);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex.ToString());
          }
        }
      }

      await Task.CompletedTask;
    }

    private bool HasSubscriptionsForEvent(string messageType)
    {
      return _subscriptions.Any(s => s.Value.MessageTypeName == messageType);
    }

    private void DoInternalSubscription(string messageType)
    {
      var containsKey = HasSubscriptionsForEvent(messageType);
      if (!containsKey)
      {
        if (!_persistentConnection.IsConnected)
        {
          _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
          channel.QueueBind(
            queue: _queueName,
            exchange: _brokerName,
            routingKey: messageType
          );
        }
      }
    }

    private class Subscription
    {
      private Type _concreteHandler;
      private object _handler;

      private Type _handlerType { get; }
      private Type _messageType { get; }

      public Guid Id { get; }

      public Type MessageType => _messageType;

      public string MessageTypeName => _messageType.Name;

      public Subscription(Type handlerType, Type messageType)
      {
        Id = Guid.NewGuid();
        _handlerType = handlerType;
        _messageType = messageType;
      }

      public async Task Handle(IServiceProvider serviceProvider, object message)
      {
        if (this._concreteHandler == null || this._handler == null)
        {
          this._handler = serviceProvider.GetService(_handlerType);
          this._concreteHandler = typeof(IMessageHandler<>).MakeGenericType(_messageType);
        }

        await (Task)this._concreteHandler.GetMethod("Handle")
          .Invoke(this._handler, new object[] {
            message,
            default(CancellationToken)
          });
      }
    }
  }
}
