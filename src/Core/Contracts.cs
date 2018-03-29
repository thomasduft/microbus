using System;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  public interface IMessageHandler<TMessage>
  {
    /// <summary>
    /// Handles the incomming message.
    /// </summary>
    /// <param name="message"></param>
    Task Handle(TMessage message);
  }

  public interface IMessageBus
  {
    /// <summary>
    /// Subscribes a message handler.
    /// </summary>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="messageHandler"></param>
    /// <returns></returns>
    Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class;

    /// <summary>
    /// PUblishes a message.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task PublishAsync<TMessage>(
      TMessage message, 
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class;

    /// <summary>
    /// Unsubscribes a subscription.
    /// </summary>
    /// <param name="subscription"></param>
    void Unsubscribe(Guid subscription);
  }
}
