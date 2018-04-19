using System;
using tomware.Microbus.Core;
using System.Threading.Tasks;
using System.Threading;
using RabbitMQ.Messages;
using RabbitMQ.MessageBus;

namespace tomware.Microbus.RabbitMQ.Subscriber
{
  class Program
  {
    static void Main(string[] args)
    {
      var messageMessageHandler = new MessageMessageHandler();
      var dispatchMessageMessageHandler = new DispatchMessageMessageHandler();
      var bus = new RabbitMQMessageBus();

      // bus.Subscribe<MessageMessageHandler, Message>(messageMessageHandler);
      bus.Subscribe<DispatchMessageMessageHandler, DispatchMessage>();

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
