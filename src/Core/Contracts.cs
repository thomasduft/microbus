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
    /// <param name="token"></param>
    Task Handle(
      TMessage message,
      CancellationToken token = default(CancellationToken));
  }

  public interface IMessageBus
  {
    /// <summary>
    /// Subscribes a message handler.
    /// </summary>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    Guid Subscribe<THandler, TMessage>()
      where THandler : IMessageHandler<TMessage>
      where TMessage : class;

    /// <summary>
    /// Publishes a message.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)) where TMessage : class;

    /// <summary>
    /// Unsubscribes a subscription.
    /// </summary>
    /// <param name="subscription"></param>
    /// <returns></returns>
    bool Unsubscribe(Guid subscription);
  }
}
