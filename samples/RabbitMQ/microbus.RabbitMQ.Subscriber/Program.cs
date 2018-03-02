using System;
using tomware.Microbus.Core;
using tomware.Microbus.RabbitMQ.Messages;

namespace tomware.Microbus.RabbitMQ.Subscriber
{
  class Program
  {
    static void Main(string[] args)
    {
      var handler = new MyMessageHandler();
      var bus = new RabbitMQMessageBus();

      bus.Subscribe<MyMessageHandler, Message>(handler);
      
      Console.WriteLine("Waiting for messages...");
      Console.ReadKey();
    }
  }

  public class MyMessageHandler : IMessageHandler<Message>
  {
    public void Handle(Message message)
    {
      Console.WriteLine($"Message received: {message.Name}");
    }
  }
}
