using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.WebApi.MessageHandlers;
using tomware.Microbus.Core;
using RabbitMQ.Messages;

namespace Microsoft.AspNetCore.Builder
{
  public static class MessageHandlerRegistrar
  {
    public static IApplicationBuilder UseMessageHandlers(this IApplicationBuilder app)
    {
      var messageBus = app.ApplicationServices.GetRequiredService<IMessageBus>();

      var dispatcher = app.ApplicationServices.GetRequiredService<DispatchMessageHandler>();
      messageBus.Subscribe<DispatchMessageHandler, Message>(dispatcher);

      return app;
    }
  }
}