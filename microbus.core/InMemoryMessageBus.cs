using System;
using System.Collections.Concurrent;
using System.Linq;

namespace tomware.Microbus.Core
{
  public class InMemoryMessageBus : IMessageBus
  {
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions
      = new ConcurrentDictionary<Guid, Subscription>();

    public Guid Subscribe<T>(IMessageHandler messageHandler) where T : IMessage
    {
      var messageType = typeof(T).FullName;
      var subscription = new Subscription(messageType, messageHandler);

      _subscriptions.TryAdd(subscription.Id, subscription);

      return subscription.Id;
    }

    public void Publish<T>(T message) where T : IMessage
    {
      var messageType = typeof(T).FullName;
      var subscriptions = _subscriptions.Values.Where(s => s.Name == messageType);
      foreach (var subscription in subscriptions)
      {
        subscription.Handler.HandleMessage(message);
      }
    }

    public void Unsubscribe(Guid subscription)
    {
      _subscriptions.TryRemove(subscription, out Subscription sub);
    }

    private class Subscription
    {
      public Guid Id { get; }
      public string Name { get; }
      public IMessageHandler Handler { get; }

      public Subscription(string name, IMessageHandler messageHandler)
      {
        Id = Guid.NewGuid();
        Name = name;
        Handler = messageHandler;
      }
    }
  }
}
