using System;

namespace tomware.Microbus.RabbitMQ.Messages
{
  public class DispatchMessage
  {
    public Guid Id { get; set; }
    public Message OriginalMessage { get; set; }

    public static DispatchMessage FromMessage(Message message)
    {
      return new DispatchMessage
      {
        Id = Guid.NewGuid(),
        OriginalMessage = message
      };
    }

    public override string ToString()
    {
      return $"{Id} - {OriginalMessage.ToString()}";
    }
  }
}
