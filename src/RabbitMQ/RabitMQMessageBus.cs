using EasyNetQ;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace tomware.Microbus.RabbitMQ
{
  public class RabbitMQMessageBus : IMessageBus
  {
    private IBus _bus;
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;

    public RabbitMQMessageBus()
    {
      _bus = RabbitHutch.CreateBus("host=localhost:5672;username=guest;password=guest");
      _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
    }

    public Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      return _bus.PublishAsync(message);
    }

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(messageHandler);

      _subscriptions.TryAdd(subscription.Id, subscription);

      // subscription strategy based on configuration?!
      // see: https://github.com/EasyNetQ/EasyNetQ/wiki/Subscribe
      var subscriptionId = subscription.Id.ToString();
      subscription.SubscriptionResult
        = _bus.Subscribe<TMessage>(
            subscriptionId,
            x => subscription.GetHandler<TMessage>().Handle(x)
          );

      return subscription.Id;
    }

    public void Unsubscribe(Guid subscriptionId)
    {
      if (_subscriptions.TryRemove(subscriptionId, out Subscription sub))
      {
        sub.SubscriptionResult.Dispose();
      }
    }

    private class Subscription
    {
      private object Handler { get; }

      public Guid Id { get; }

      public ISubscriptionResult SubscriptionResult { get; set; }

      public Subscription(object handler)
      {
        Id = Guid.NewGuid();
        Handler = handler;
      }

      public IMessageHandler<TMessage> GetHandler<TMessage>()
      {
        return Handler as IMessageHandler<TMessage>;
      }
    }
  }
}
