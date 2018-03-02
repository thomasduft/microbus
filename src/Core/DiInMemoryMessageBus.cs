using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tomware.Microbus.Core
{
  // TODO: Work in Progress
  public class DiInMemoryMessageBus : IMessageBus
  {
    private readonly IServiceProvider _serviceProvider;

    public DiInMemoryMessageBus(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;
    }

    public Guid Subscribe<THandler, TMessage>(THandler messageHandler)
      where THandler : IMessageHandler<TMessage>
      where TMessage : class
    {
      /**
       * registering done via di framework during Startup.ConfigureServices
       * public void ConfigureServices(IServiceCollection services)
       * 
       * Sample:
       *  ...
       *  services.AddMvc();
       *
       *  services.AddSingleton<IMessageBus, DiInMemoryMessageBus>();
       *  services.AddTransient<IMessageHandler, MyMessageHandler>();
       *  ...
       *  
       */

      throw new NotImplementedException();
    }

    public async Task PublishAsync<TMessage>(
      TMessage message,
      CancellationToken token = default(CancellationToken)
    ) where TMessage : class
    {
      var list = new List<Task>();
      IEnumerable<IMessageHandler<TMessage>> handlers
        = (IEnumerable<IMessageHandler<TMessage>>)_serviceProvider
        .GetService(typeof(IMessageHandler<TMessage>));

      foreach (var handler in handlers)
      {
        if (token.IsCancellationRequested) break;

        try
        {

          list.Add(Task.Run(() => handler.Handle(message)));
        }
        catch
        {
          continue;
        }
      }

      await Task.WhenAll(list);
    }

    public void Unsubscribe(Guid subscription)
    {
      throw new NotImplementedException();
    }
  }
}
