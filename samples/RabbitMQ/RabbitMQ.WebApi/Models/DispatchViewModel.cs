using System;
using System.ComponentModel.DataAnnotations;
using tomware.Microbus.RabbitMQ.Messages;

namespace tomware.Microbus.RabbitMQ.WebApi.Models
{
  public class DispatchViewModel
  {
    [Required]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public DispatchMessage ToDispatchMessage()
    {
      return new DispatchMessage
      {
        Id = Guid.NewGuid(),
        OriginalMessage = new Message
        {
          Id = Id,
          Name = Name
        }
      };
    }
  }
}
