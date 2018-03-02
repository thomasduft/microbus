using System;
using tomware.Microbus.RabbitMQ.Messages;

namespace tomware.Microbus.RabbitMQ.Publisher
{
  class Program
  {
    static void Main(string[] args)
    {
      var bus = new RabbitMQMessageBus();
      var messages = 5000;
      for (int i = 0; i < messages; i++)
      {
        bus.PublishAsync(new Message
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
