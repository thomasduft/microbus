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

    private readonly IServiceProvider _serviceProvider;

    public InMemoryMessageBus(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    public Guid Subscribe<THandler, TMessage>()
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      var subscription = new Subscription(typeof(THandler), typeof(TMessage));

      _subscriptions.TryAdd(subscription.Id, subscription);

      return subscription.Id;
    }

    public async Task PublishAsync<TMessage>(
     TMessage message,
     CancellationToken token = default(CancellationToken)
   ) where TMessage : class
    {
      if (message == null) throw new ArgumentNullException(nameof(message));

      var subscriptionsForMessageType = _subscriptions.Values
        .Where(_ => _.MessageType == typeof(TMessage));

      var tasks = new List<Task>();
      foreach (var subscription in subscriptionsForMessageType)
      {
        if (token.IsCancellationRequested) break;

        try
        {
          tasks.Add(subscription.Handle(_serviceProvider, message, token));
        }
        catch
        {
          continue;
        }
      }

      await Task.WhenAll(tasks);
    }

    public bool Unsubscribe(Guid subscription)
    {
      Subscription sub = null;
      return _subscriptions.TryRemove(subscription, out sub);
    }

    private class Subscription
    {
      private Type _handlerType { get; }
      private Type _messageType { get; }

      public Guid Id { get; }

      public Type MessageType => _messageType;

      public Subscription(Type handlerType, Type messageType)
      {
        Id = Guid.NewGuid();
        _handlerType = handlerType;
        _messageType = messageType;
      }

      internal Task Handle<TMessage>(
        IServiceProvider serviceProvider,
        TMessage message,
        CancellationToken token
      )
      {
        var handler = serviceProvider.GetService(_handlerType) as IMessageHandler<TMessage>;

        return handler.Handle(message, token);
      }
    }
  }
}
