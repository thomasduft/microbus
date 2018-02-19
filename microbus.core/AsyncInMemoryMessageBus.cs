using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  public class AsyncInMemoryMessageBus : IAsyncMessageBus
  {
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions
      = new ConcurrentDictionary<Guid, Subscription>();
    private readonly ConcurrentQueue<Subscription> _pendingSubscriptions
      = new ConcurrentQueue<Subscription>();
    private readonly ConcurrentQueue<Subscription> _pendingUnsubscriptions
      = new ConcurrentQueue<Subscription>();

    public Guid Subscribe<T>(IMessageHandler messageHandler) where T : IMessage
    {
      var messageType = typeof(T).FullName;
      var subscription = new Subscription(messageType, messageHandler);

      _pendingSubscriptions.Enqueue(subscription);

      return subscription.Id;
    }

    public async Task PublishAsync<T>(
      T message,
      CancellationToken token = default(CancellationToken)
    ) where T : IMessage
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

      var list = new List<Task> { Task.FromResult(0) };
      foreach (var subscription in _subscriptions.Values.ToList())
      {
        if (token.IsCancellationRequested)
        {
          break;
        }

        try
        {
          list.Add(new Task(() => subscription.Handler.HandleMessage(message)));
        }
        catch
        {
          continue;
        }
      }

      await Task.WhenAll(list);
    }

    public void Publish<T>(T message) where T : IMessage
    {
      var messageType = typeof(T).FullName;
      var subscriptions = this._subscriptions.Values.Where(s => s.Name == messageType);
      foreach (var subscription in subscriptions)
      {
        subscription.Handler.HandleMessage(message);
      }
    }

    public void Unsubscribe(Guid id)
    {
      var subscription = _pendingSubscriptions.FirstOrDefault(s => s.Id == id);
      _pendingUnsubscriptions.Enqueue(subscription);
    }

    private class Subscription
    {
      public Guid Id { get; }
      public string Name { get; }
      public IMessageHandler Handler { get; }

      public Subscription(string name, IMessageHandler messageHandler)
      {
        this.Id = Guid.NewGuid();
        this.Name = name;
        this.Handler = messageHandler;
      }
    }
  }
}
