using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.MessageBus;
using RabbitMQ.Messages;
using tomware.Microbus.Core;

namespace RabbitMQ.Publisher
{
  class Program
  {
    static void Main(string[] args)
    {
      IServiceCollection services = new ServiceCollection();
      services.AddLogging();

      services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
      services.AddSingleton<IRabbitMQMessageBusConfiguration, DefaultRabbitMQMessageBusConfiguration>(
        ctx => new DefaultRabbitMQMessageBusConfiguration(
          "ProgrammPublisher",
          "host=localhost;username=guest;password=guest",
        5)
      );
      services.AddSingleton<IMessageBus, RabbitMQMessageBus>();

      IServiceProvider provider = services.BuildServiceProvider();
      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();

      var messages = 5000;
      for (int i = 0; i < messages; i++)
      {
        messageBus.PublishAsync(new Message
        {
          Id = i,
          Name = $"MyNumberIs_{i}"
        });
      }

      Console.WriteLine($"Sent {messages} messages...");
      Console.Read();
    }
  }
}
