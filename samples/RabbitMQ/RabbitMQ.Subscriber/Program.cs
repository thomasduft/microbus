using System;
using tomware.Microbus.Core;
using System.Threading.Tasks;
using System.Threading;
using RabbitMQ.Messages;
using RabbitMQ.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace tomware.Microbus.RabbitMQ.Subscriber
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
          "ProgrammSubscriber", 
          "host=localhost;username=guest;password=guest", 
        5)
      );
      services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
      services.AddSingleton<MessageMessageHandler>();
      services.AddSingleton<DispatchMessageMessageHandler>();

      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      messageBus.Subscribe<MessageMessageHandler, Message>();
      messageBus.Subscribe<DispatchMessageMessageHandler, DispatchMessage>();

      Console.WriteLine("Waiting for messages...");
      Console.Read();
    }
  }

  public class MessageMessageHandler : IMessageHandler<Message>
  {
    public async Task Handle(
      Message message,
      CancellationToken token = default(CancellationToken)
    )
    {
      Console.WriteLine($"Message received: {message}");

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
