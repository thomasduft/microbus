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

    public async Task PublishAsync<T>(
      T message,
      CancellationToken token = default(CancellationToken)
    ) where T : class
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
      foreach (var subscription in _subscriptions.Values.ToList())
      {
        if (token.IsCancellationRequested)
        {
          break;
        }

        try
        {
          list.Add(Task.Run(() => subscription.GetHandler<T>().Handle(message)));
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
      private object Handler { get; }

      public Guid Id { get; }
      public string Name { get; }

      public Subscription(string name, object handler)
      {
        Id = Guid.NewGuid();
        Name = name;
        Handler = handler;
      }

      public IMessageHandler<TMessage> GetHandler<TMessage>() {
        return Handler as IMessageHandler<TMessage>;
      }
    }
  }
}
