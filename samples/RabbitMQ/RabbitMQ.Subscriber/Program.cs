using System;
using tomware.Microbus.Core;
using RabbitMQ.Messages;
using System.Threading.Tasks;

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
      bus.Subscribe<DispatchMessageMessageHandler, DispatchMessage>(dispatchMessageMessageHandler);

      Console.WriteLine("Waiting for messages...");
      Console.ReadKey();
    }
  }

  public class MessageMessageHandler : IMessageHandler<Message>
  {
    public async Task Handle(Message message)
    {
      Console.WriteLine($"Message received: {message}");

      await Task.FromResult(0);
    }
  }

  public class DispatchMessageMessageHandler : IMessageHandler<DispatchMessage>
  {
    public async Task Handle(DispatchMessage message)
    {
      Console.WriteLine($"DispatchMessage received: {message}");

      await Task.FromResult(0);
    }
  }
}
