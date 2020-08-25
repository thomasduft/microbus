using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.MessageBus;
using RabbitMQ.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace RabbitMQ.Subscriber
{
  class Program
  {
    static void Main(string[] args)
    {
      IServiceCollection services = new ServiceCollection();
      services
        .AddLogging(cfg => cfg.AddConsole())
        .Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Trace);

      services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
      services.Configure<RabbitMQMessageBusConfiguration>(opt =>
        {
          opt.ClientName = "Subscriber";
          opt.QueueName = "tw.Subscriber";
          opt.BrokerName = "tw.messages";
          opt.BrokerStrategy = "fanout";
          opt.ConnectionString = "host=localhost;username=guest;password=guest";
          opt.RetryCount = 5;
          opt.ConfirmSelect = false;
          opt.Persistent = false;
          opt.AutoAck = true;
        });
      services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
      services.AddSingleton<MessageMessageHandler>();
      services.AddSingleton<DispatchMessageMessageHandler>();

      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      messageBus.Subscribe<MessageMessageHandler, Message>();
      messageBus.Subscribe<DispatchMessageMessageHandler, DispatchMessage>();
      ILogger logger = provider.GetService<ILogger<Program>>();

      logger.LogInformation("Waiting for messages...");
      Console.Read();
    }
  }

  public class MessageMessageHandler : IMessageHandler<Message>
  {
    private int sleep = 0;

    public async Task Handle(
      Message message,
      CancellationToken token = default(CancellationToken)
    )
    {
      Console.WriteLine($"Message received: {message}");
      if (this.sleep > 0)
      {
        Thread.Sleep(this.sleep);
      }

      await Task.CompletedTask;
    }
  }

  public class DispatchMessageMessageHandler : IMessageHandler<DispatchMessage>
  {
    public async Task Handle(
      DispatchMessage message,
      CancellationToken token = default(CancellationToken)
    )
    {
      Console.WriteLine($"DispatchMessage received: {message}");

      await Task.CompletedTask;
    }
  }
}
