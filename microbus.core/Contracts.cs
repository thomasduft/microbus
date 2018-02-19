using System;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  public interface IMessage { }

  public interface IMessageHandler
  {
    void HandleMessage(IMessage message);
  }

  public interface IMessageBus
  {
    Guid Subscribe<T>(IMessageHandler messageHandler) where T : IMessage;
    void Publish<T>(T message) where T : IMessage;
    void Unsubscribe(Guid subscription);
  }

  public interface IAsyncMessageBus
  {
    Guid Subscribe<T>(IMessageHandler messageHandler) where T : IMessage;
    Task PublishAsync<T>(T message, CancellationToken token = default(CancellationToken)) where T : IMessage;
    void Unsubscribe(Guid subscription);
  }
}
