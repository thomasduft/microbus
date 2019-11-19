using Microsoft.Extensions.DependencyInjection;
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
      services.AddLogging();

      services.AddSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
      services.Configure<RabbitMQMessageBusConfiguration>(opt =>
      {
        opt.ClientName = "Publisher";
        opt.QueueName = "tw.Publisher";
        opt.BrokerName = "tw.messages";
        opt.BrokerStrategy = "fanout";
        opt.ConnectionString = "host=localhost;username=guest;password=guest";
        opt.RetryCount = 5;
        opt.ConfirmSelect = false;
      });
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
