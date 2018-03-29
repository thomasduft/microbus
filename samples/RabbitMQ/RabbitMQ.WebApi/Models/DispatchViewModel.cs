using System;
using System.ComponentModel.DataAnnotations;
using RabbitMQ.Messages;

namespace RabbitMQ.WebApi.Models
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
