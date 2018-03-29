using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using tomware.Microbus.Core;
using RabbitMQ.WebApi.Models;

namespace RabbitMQ.WebApi.Services
{
  public interface IDispatchService
  {
    Task SendAsync(DispatchViewModel model);
  }

  public class DispatchService : IDispatchService
  {
    private readonly ILogger<DispatchService> _logger;
    private readonly IMessageBus _messageBus;

    public DispatchService(ILoggerFactory loggerFactory, IMessageBus messageBus)
    {
      _logger = loggerFactory.CreateLogger<DispatchService>();
      _messageBus = messageBus;
    }

    public Task SendAsync(DispatchViewModel model)
    {
      var dispatchMessage = model.ToDispatchMessage();

      _logger.LogInformation($"Sending dispatch message {dispatchMessage}...");

      return _messageBus.PublishAsync(dispatchMessage);
    }
  }
}
