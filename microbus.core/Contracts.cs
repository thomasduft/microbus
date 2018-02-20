using System;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  public interface IMessageHandler<TMessage>
  {
    void Handle(TMessage message);
  }

  public interface IMessageBus
  {
    Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class;
    Task PublishAsync<T>(T message, CancellationToken token = default(CancellationToken)) 
      where T : class;
    void Unsubscribe(Guid subscription);
  }
}
