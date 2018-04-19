using Microsoft.Extensions.Logging;
using RabbitMQ.Messages;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace RabbitMQ.WebApi.MessageHandlers
{
  public class DispatchMessageHandler : IMessageHandler<Message>
  {
    private readonly ILogger<DispatchMessageHandler> _logger;
    private readonly IMessageBus _messageBus;

    public DispatchMessageHandler(ILogger<DispatchMessageHandler> logger, IMessageBus messageBus)
    {
      _logger = logger;
      _messageBus = messageBus;
    }

    public async Task Handle(Message message, CancellationToken token = default(CancellationToken))
    {
      _logger.LogInformation($"Receiving message {message}");

      var dispatchMessage = DispatchMessage.FromMessage(message);

      _logger.LogInformation($"Sending dispatch message {dispatchMessage}...");
      
      await _messageBus.PublishAsync(dispatchMessage);
    }
  }
}
