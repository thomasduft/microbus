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

      var clientName = "ProgrammSubscriber";
      var connection = "host=localhost:5672;username=u23567;password=pw";

      services.AddSingleton<IMessageBus, RabbitMQMessageBus>((ctx) =>
      {
        return new RabbitMQMessageBus(
          new LoggerFactory().CreateLogger<RabbitMQMessageBus>(),
          new DefaultRabbitMQPersistentConnection(
            RabbitMQMessageBus.CreateConnectionFactory(connection),
            clientName,
             new LoggerFactory().CreateLogger<DefaultRabbitMQPersistentConnection>()
          ),
          clientName
        );
      });

      services.AddSingleton<MessageMessageHandler>();
      services.AddSingleton<DispatchMessageMessageHandler>();
      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      messageBus.Subscribe<MessageMessageHandler, Message>();
      messageBus.Subscribe<DispatchMessageMessageHandler, DispatchMessage>();

      Console.WriteLine("Waiting for messages...");
      Console.ReadKey();
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
