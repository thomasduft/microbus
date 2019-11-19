using EasyNetQ.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using System;
using tomware.Microbus.Core;

namespace EasyNetQ.Publisher
{
  class Program
  {
    static void Main(string[] args)
    {
      IServiceCollection services = new ServiceCollection();
      services.AddLogging();

      services.AddSingleton<IMessageBus, EasyNetQMessageBus>();

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