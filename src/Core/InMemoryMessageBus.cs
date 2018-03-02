using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  public class InMemoryMessageBus : IMessageBus
  {
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions
      = new ConcurrentDictionary<Guid, Subscription>();
    private readonly ConcurrentQueue<Subscription> _pendingSubscriptions
      = new ConcurrentQueue<Subscription>();
    private readonly ConcurrentQueue<Subscription> _pendingUnsubscriptions
      = new ConcurrentQueue<Subscription>();

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var messageType = typeof(TMessage).FullName;
      var subscription = new Subscription(messageType, messageHandler);

      _pendingSubscriptions.Enqueue(subscription);

      return subscription.Id;
    }

    public async Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      Subscription sub;
      while (_pendingUnsubscriptions.TryDequeue(out sub))
      {
        _subscriptions.TryRemove(sub.Id, out sub);
      }

      while (_pendingSubscriptions.TryDequeue(out sub))
      {
        _subscriptions.TryAdd(sub.Id, sub);
      }

      var list = new List<Task>();
      var messageType = typeof(TMessage).FullName;
      var subscriptionsForMessageType = _subscriptions.Values
        .Where(x => x.MessageType == messageType);
      foreach (var subscription in subscriptionsForMessageType)
      {
        if (token.IsCancellationRequested) break;

        try
        {
          list.Add(Task.Run(() => subscription.GetHandler<TMessage>().Handle(message)));
        }
        catch
        {
          continue;
        }
      }

      await Task.WhenAll(list);
    }

    public void Unsubscribe(Guid id)
    {
      var subscription = _subscriptions.FirstOrDefault(s => s.Key == id);
      _pendingUnsubscriptions.Enqueue(subscription.Value);
    }

    private class Subscription
    {
      private object _handler { get; }

      public Guid Id { get; }
      public string MessageType { get; }

      public Subscription(string messageType, object handler)
      {
        Id = Guid.NewGuid();
        MessageType = messageType;
        _handler = handler;
      }

      public IMessageHandler<TMessage> GetHandler<TMessage>()
      {
        return _handler as IMessageHandler<TMessage>;
      }
    }
  }
}
