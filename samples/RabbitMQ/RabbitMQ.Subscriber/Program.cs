using System;
using tomware.Microbus.Core;
using tomware.Microbus.RabbitMQ.Messages;

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
    public void Handle(Message message)
    {
      Console.WriteLine($"Message received: {message}");
    }
  }

  public class DispatchMessageMessageHandler : IMessageHandler<DispatchMessage>
  {
    public void Handle(DispatchMessage message)
    {
      Console.WriteLine($"DispatchMessage received: {message}");
    }
  }
}
