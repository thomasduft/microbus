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

    public const string QUEUE_NAME = "the_queue";

    public RabbitMQMessageBus()
    {
      _bus = RabbitHutch.CreateBus("host=localhost:5672;username=guest;password=guest");
      _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken token = default(CancellationToken)) where TMessage : class
    {
       return _bus.SendAsync(QUEUE_NAME, message);
    }

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(messageHandler);

      _subscriptions.TryAdd(subscription.Id, subscription);

      _bus.Receive<TMessage>(QUEUE_NAME, x => subscription.GetHandler<TMessage>().Handle(x));

      return subscription.Id;
    }

    public void Unsubscribe(Guid subscription)
    {
      _subscriptions.TryRemove(subscription, out Subscription sub);
    }

    private class Subscription
    {
      private object Handler { get; }

      public Guid Id { get; }

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
