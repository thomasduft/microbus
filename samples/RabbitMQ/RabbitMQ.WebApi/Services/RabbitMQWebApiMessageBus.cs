using EasyNetQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace RabbitMQ.WebApi.Services
{
  public class RabbitMQWebApiMessageBus : IMessageBus
  {
    private readonly string _client;
    private readonly IBus _bus;
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;

    public RabbitMQWebApiMessageBus(IConfiguration configuration)
    {
      _client = configuration["Client"];
      var connection = configuration["RabbitMQ:Connection"];

      _bus = RabbitHutch.CreateBus(connection);
      _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
    }

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(messageHandler);

      _subscriptions.TryAdd(subscription.Id, subscription);

      // subscription strategy based on configuration?!
      // see: https://github.com/EasyNetQ/EasyNetQ/wiki/Subscribe
      var subscriptionId = _client; // subscription.Id.ToString();

      subscription.SubscriptionResult
        = _bus.Subscribe<TMessage>(subscriptionId,
          x => subscription.GetHandler<TMessage>().Handle(x));

      return subscription.Id;
    }

    public Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      return _bus.PublishAsync(message);
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
      private object _handler { get; }

      public Guid Id { get; }

      public ISubscriptionResult SubscriptionResult { get; set; }

      public Subscription(object handler)
      {
        Id = Guid.NewGuid();
        _handler = handler;
      }

      public IMessageHandler<TMessage> GetHandler<TMessage>()
      {
        return _handler as IMessageHandler<TMessage>;
      }
    }
  }
}
