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
    Task PublishAsync<TMessage>(TMessage message, CancellationToken token = default(CancellationToken)) 
      where TMessage : class;
    void Unsubscribe(Guid subscription);
  }
}
