using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.MessageBus;
using RabbitMQ.Messages;
using System;
using tomware.Microbus.Core;

namespace RabbitMQ.Publisher
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
        opt.ClientName = "Publisher";
        opt.QueueName = "tw.Publisher";
        opt.BrokerName = "tw.messages";
        opt.BrokerStrategy = "fanout";
        opt.ConnectionString = "host=localhost;username=guest;password=guest";
        opt.RetryCount = 5;
        opt.ConfirmSelect = true;
      });
      services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

      IServiceProvider provider = services.BuildServiceProvider();
      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      ILogger logger = provider.GetService<ILogger<Program>>();

      var messages = 100;
      for (int i = 0; i < messages; i++)
      {
        messageBus.PublishAsync(new Message
        {
          Id = i,
          Name = $"MyNumberIs_{i}"
        });
      }

      logger.LogInformation($"{messages} messages sent...");
      Console.Read();
    }
  }
}
