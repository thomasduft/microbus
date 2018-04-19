using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace tomware.Microbus.Tests
{
  [TestClass]
  public class InMemoryMessageBusTest
  {
    [TestMethod]
    public async Task PublishAsync_SampleMessage_MessageHandlerHasSampleMessageReceived()
    {
      // Arrange
      IServiceCollection services = new ServiceCollection();
      services.AddSingleton<IMessageBus, InMemoryMessageBus>();
      services.AddSingleton<SampleMessageHandler>();
      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      SampleMessageHandler sampleMessageHandler = provider.GetRequiredService<SampleMessageHandler>();
      messageBus.Subscribe<SampleMessageHandler, SampleMessage>();

      var sampleMessage = new SampleMessage("Hello Test");

      // Act
      await messageBus.PublishAsync(sampleMessage);

      // Assert
      Assert.AreEqual("Hello Test", sampleMessageHandler.MyMessage);
    }

    [TestMethod]
    public async Task PublishAsync_IncreaseMessageInitialValueIs0_FinalValueIs42()
    {
      // Arrange
      IServiceCollection services = new ServiceCollection();
      services.AddSingleton<IMessageBus, InMemoryMessageBus>();
      services.AddSingleton<IncreaseMessageHandler>();
      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      IncreaseMessageHandler handler = provider.GetRequiredService<IncreaseMessageHandler>();
      messageBus.Subscribe<IncreaseMessageHandler, IncreaseMessage>();

      var firstMessage = new IncreaseMessage(40);
      var secondMessage = new IncreaseMessage(2);

      // Act
      await messageBus.PublishAsync(firstMessage);
      await messageBus.PublishAsync(secondMessage);

      // Assert
      Assert.AreEqual(42, handler.Value);
    }

    [TestMethod]
    public async Task Unsubscribe_SampleMessage_NoMessageReceived()
    {
      // Arrange
      IServiceCollection services = new ServiceCollection();
      services.AddSingleton<IMessageBus, InMemoryMessageBus>();
      services.AddSingleton<SampleMessageHandler>();
      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      SampleMessageHandler sampleMessageHandler = provider.GetRequiredService<SampleMessageHandler>();
      var subscription = messageBus.Subscribe<SampleMessageHandler, SampleMessage>();

      await messageBus.PublishAsync(new SampleMessage("Hello Test"));
      Assert.AreEqual("Hello Test", sampleMessageHandler.MyMessage);

      // Act
      messageBus.Unsubscribe(subscription);

      // Assert
      await messageBus.PublishAsync(new SampleMessage("Hello Echo"));

      Assert.AreEqual("Hello Test", sampleMessageHandler.MyMessage);
    }

    [TestMethod]
    public async Task PublishAsync_SampleMessage_IncreaseMessageHandlerShouldNotReceiveMessage()
    {
      // Arrange
      IServiceCollection services = new ServiceCollection();
      services.AddSingleton<IMessageBus, InMemoryMessageBus>();
      services.AddSingleton<SampleMessageHandler>();
      services.AddSingleton<ToLowerSampleMessageHandler>();
      services.AddSingleton<IncreaseMessageHandler>();
      IServiceProvider provider = services.BuildServiceProvider();

      IMessageBus messageBus = provider.GetRequiredService<IMessageBus>();
      SampleMessageHandler sampleMessageHandler = provider.GetRequiredService<SampleMessageHandler>();
      ToLowerSampleMessageHandler toLowerSampleMessageHandler = provider.GetRequiredService<ToLowerSampleMessageHandler>();
      IncreaseMessageHandler increaseMessageHandler = provider.GetRequiredService<IncreaseMessageHandler>();

      messageBus.Subscribe<SampleMessageHandler, SampleMessage>();
      messageBus.Subscribe<ToLowerSampleMessageHandler, SampleMessage>();
      messageBus.Subscribe<IncreaseMessageHandler, IncreaseMessage>();

      var sampleMessage = new SampleMessage("Hello Test");

      // Act
      await messageBus.PublishAsync(sampleMessage);

      // Assert
      Assert.AreEqual("Hello Test", sampleMessageHandler.MyMessage);
      Assert.AreEqual("hello test", toLowerSampleMessageHandler.MyMessage);
      Assert.AreEqual(0, increaseMessageHandler.Value);
    }
  }

  public class SampleMessage
  {
    public string Message { get; }

    public SampleMessage(string message)
    {
      Message = message;
    }
  }

  public class SampleMessageHandler : IMessageHandler<SampleMessage>
  {
    public string MyMessage { get; set; }

    public SampleMessageHandler()
    {
      MyMessage = string.Empty;
    }

    public async Task Handle(
      SampleMessage message,
      CancellationToken token = default(CancellationToken)
    )
    {
      MyMessage = message.Message;

      await Task.CompletedTask;
    }
  }

  public class ToLowerSampleMessageHandler : IMessageHandler<SampleMessage>
  {
    public string MyMessage { get; set; }

    public ToLowerSampleMessageHandler()
    {
      MyMessage = "ToLower";
    }

    public async Task Handle(
      SampleMessage message,
      CancellationToken token = default(CancellationToken)
    )
    {
      MyMessage = message.Message.ToLower();

      await Task.CompletedTask;
    }
  }

  public class IncreaseMessage
  {
    public int Increase { get; }

    public IncreaseMessage(int increase)
    {
      Increase = increase;
    }
  }

  public class IncreaseMessageHandler : IMessageHandler<IncreaseMessage>
  {
    public int Value { get; set; }

    public IncreaseMessageHandler()
    {
      Value = 0;
    }

    public async Task Handle(
      IncreaseMessage message,
      CancellationToken token = default(CancellationToken)
    )
    {
      Value += message.Increase;

      await Task.CompletedTask;
    }
  }
}
