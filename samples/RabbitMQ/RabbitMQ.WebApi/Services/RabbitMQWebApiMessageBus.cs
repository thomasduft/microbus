using EasyNetQ;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace tomware.Microbus.RabbitMQ.WebApi.Services
{
  public class RabbitMQWebApiMessageBus : IMessageBus
  {
    private readonly System.IServiceProvider _serviceProvider;
    private readonly IBus _bus;
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions;


    public RabbitMQWebApiMessageBus(System.IServiceProvider serviceProvider, IConfiguration configuration)
    {
      var connection = configuration["RabbitMQ:Connection"];

      _serviceProvider = serviceProvider;
      _bus = RabbitHutch.CreateBus(connection);
      _subscriptions = new ConcurrentDictionary<Guid, Subscription>();
    }

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      /**
       * Registering done via di framework during Startup.ConfigureServices.
       * 
       * Sample:
       * public void ConfigureServices(IServiceCollection services) {
       *  ...
       *  services.AddSingleton<IMessageBus, RabbitMQWebApiMessageBus>();
       *  services.AddTransient<IMessageHandler, MyMessageHandler>();
       *  ...
       * }
       *
       * public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
       *  ...
       *  var messageBus = app.ApplicationServices.GetRequiredService<IMessageBus>();
       *  messageBus.Subscribe<DispatchMessageHandler, Message>(null);
       *  ...
       * }
       */

      var subscription = new Subscription();

      _subscriptions.TryAdd(subscription.Id, subscription);

      // subscription strategy based on configuration?!
      // see: https://github.com/EasyNetQ/EasyNetQ/wiki/Subscribe
      var subscriptionId = subscription.Id.ToString();

      IMessageHandler<TMessage> handler
        = (IMessageHandler<TMessage>)_serviceProvider.GetService(typeof(THandler));

      subscription.SubscriptionResult
        = _bus.Subscribe<TMessage>(subscriptionId, x => handler.Handle(x));

      return subscription.Id;
    }

    public Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      return _bus.PublishAsync(message);
    }

    public void Unsubscribe(Guid subscription)
    {
      throw new NotImplementedException();
    }

    private class Subscription
    {
      public Guid Id { get; }

      public ISubscriptionResult SubscriptionResult { get; set; }

      public Subscription()
      {
        Id = Guid.NewGuid();
      }
    }
  }
}
