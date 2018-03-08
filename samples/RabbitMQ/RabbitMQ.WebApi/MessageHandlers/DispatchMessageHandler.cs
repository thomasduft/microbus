using Microsoft.Extensions.Logging;
using tomware.Microbus.Core;
using tomware.Microbus.RabbitMQ.Messages;

namespace tomware.Microbus.RabbitMQ.WebApi.MessageHandlers
{
  public class DispatchMessageHandler : IMessageHandler<Message>
  {
    private readonly ILogger<DispatchMessageHandler> _logger;
    private readonly IMessageBus _messageBus;

    public DispatchMessageHandler(ILoggerFactory loggerFactory, IMessageBus messageBus)
    {
      _logger = loggerFactory.CreateLogger<DispatchMessageHandler>();
      _messageBus = messageBus;
    }

    public void Handle(Message message)
    {
      _logger.LogInformation($"Receiving message {message}");

      var dispatchMessage = DispatchMessage.FromMessage(message);

      _logger.LogInformation($"Sending dispatch message {dispatchMessage}...");
      _messageBus.PublishAsync(dispatchMessage);
    }
  }
}
