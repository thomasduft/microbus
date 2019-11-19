using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace EasyNetQ.MessageBus
{
  public class EasyNetQMessageBus : IMessageBus
  {
    private readonly ILogger<EasyNetQMessageBus> logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBus _bus;
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;

    public EasyNetQMessageBus(
      ILogger<EasyNetQMessageBus> logger,
      IServiceProvider serviceProvider
    )
    {
      this.logger = logger;
      this._serviceProvider = serviceProvider;
      this._bus = RabbitHutch.CreateBus("host=localhost;username=guest;password=guest;publisherConfirms=true;timeout=10");
      this._subscriptions = new ConcurrentDictionary<Guid, Subscription>();
    }

    public Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      return _bus.PublishAsync(message);
    }


    public Guid Subscribe<THandler, TMessage>()
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(typeof(THandler), typeof(TMessage));
      var subscriptionId = subscription.Id.ToString();
      subscription.SubscriptionResult = this._bus.Subscribe<TMessage>(
        subscriptionId,
        async x => await subscription.Handle(this._serviceProvider, x)
      );

        // DoInternalSubscription(subscription.MessageTypeName);
      _subscriptions.TryAdd(subscription.Id, subscription);

      return subscription.Id;
    }

    public bool Unsubscribe(Guid subscriptionId)
    {
      if (_subscriptions.TryRemove(subscriptionId, out Subscription sub))
      {
        sub.SubscriptionResult.Dispose();
        return true;
      }

      return false;
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

      public ISubscriptionResult SubscriptionResult { get; set; }

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
