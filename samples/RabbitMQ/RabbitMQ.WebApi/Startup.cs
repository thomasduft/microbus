using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using tomware.Microbus.Core;
using tomware.Microbus.RabbitMQ;
using RabbitMQ.Messages;
using RabbitMQ.WebApi.MessageHandlers;
using RabbitMQ.WebApi.Services;

namespace RabbitMQ.WebApi
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc();

      // Swagger
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info
        {
          Version = "v1",
          Title = "API Documentation",
          Description = "API Documentation",
          TermsOfService = "N/A"
        });
      });

      // Services
      services.AddSingleton<IMessageBus, RabbitMQWebApiMessageBus>();
      services.AddTransient<IDispatchService, DispatchService>();

      services.AddTransient<DispatchMessageHandler>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app,
      IHostingEnvironment env,
      IApplicationLifetime appLifetime
    )
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      // Subscribing MessageHandlers
      app.UseMessageHandlers();
      appLifetime.ApplicationStopping.Register(() => OnShutdown(app));

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Modular API V1");
      });

      app.UseMvc();
    }

    private void OnShutdown(IApplicationBuilder app)
    {
      var messageBus = app.ApplicationServices.GetRequiredService<IMessageBus>();
      if (messageBus != null) {
        ((RabbitMQWebApiMessageBus)messageBus).Dispose();
      }
    }
  }
}
