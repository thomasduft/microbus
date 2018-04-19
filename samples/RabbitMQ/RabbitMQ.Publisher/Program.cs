using System;
using Microsoft.Extensions.DependencyInjection;
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
      services.AddSingleton<IMessageBus, InMemoryMessageBus>();
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
      Console.ReadKey();
    }
  }
}
