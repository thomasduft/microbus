using Microsoft.Extensions.Logging;
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
    public const string BROKER_NAME = "tw.messagebus";
    public const string BROKER_STRATEGY = "fanout";

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly int _retryCount;
    private IModel _consumerChannel;
    private string _queueName;

    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;

    public RabbitMQMessageBus(
      ILogger<RabbitMQMessageBus> logger,
      IServiceProvider serviceProvider,
      IRabbitMQPersistentConnection persistentConnection,
      IRabbitMQMessageBusConfiguration rabbitMQMessageBusConfiguration
    )
    {
      _logger = logger;
      _serviceProvider = serviceProvider;
      _persistentConnection = persistentConnection;
      _queueName = CreateQueueName(rabbitMQMessageBusConfiguration.ClientName);
      _retryCount = rabbitMQMessageBusConfiguration.RetryCount;

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

        channel.ExchangeDeclare(exchange: BROKER_NAME, type: BROKER_STRATEGY);

        var rawMessage = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(rawMessage);

        policy.Execute(() =>
        {
          var properties = channel.CreateBasicProperties();
          properties.DeliveryMode = 2; // persistent

          channel.BasicPublish(
            exchange: BROKER_NAME,
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
      _subscriptions.TryAdd(subscription.Id, subscription);
      DoInternalSubscription(subscription.MessageTypeName);

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

    private string CreateQueueName(string clientName)
    {
      return $"tw.{clientName.ToLowerInvariant()}";
    }

    private IModel CreateConsumerChannel()
    {
      if (!_persistentConnection.IsConnected)
      {
        _persistentConnection.TryConnect();
      }

      var channel = _persistentConnection.CreateModel();

      channel.ExchangeDeclare(exchange: BROKER_NAME, type: BROKER_STRATEGY);

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
            exchange: BROKER_NAME,
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

      public Type HandlerType => _handlerType;

      public Type MessageType => _messageType;

      public string HandlerTypeName => _handlerType.Name;

      public string MessageTypeName => _messageType.Name;

      public Subscription(Type handlerType, Type messageType)
      {
        Id = Guid.NewGuid();
        _handlerType = handlerType;
        _messageType = messageType;
      }

      public async Task Handle(IServiceProvider serviceProvider, object message)
      {
        if (this._concreteHandler == null || this._handler == null) {
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
