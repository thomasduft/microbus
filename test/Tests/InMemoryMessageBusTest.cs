using Microsoft.VisualStudio.TestTools.UnitTesting;
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
      IMessageBus messageBus = new InMemoryMessageBus();
      SampleMessageHandler sampleMessageHandler = new SampleMessageHandler(string.Empty);
      messageBus.Subscribe<SampleMessageHandler, SampleMessage>(sampleMessageHandler);

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
      IMessageBus messageBus = new InMemoryMessageBus();
      IncreaseMessageHandler handler = new IncreaseMessageHandler(0);
      messageBus.Subscribe<IncreaseMessageHandler, IncreaseMessage>(handler);

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
      IMessageBus messageBus = new InMemoryMessageBus();
      SampleMessageHandler sampleMessageHandler = new SampleMessageHandler(string.Empty);
      var subscription = messageBus.Subscribe<SampleMessageHandler, SampleMessage>(sampleMessageHandler);

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
      SampleMessageHandler sampleMessageHandler = new SampleMessageHandler(string.Empty);
      ToLowerSampleMessageHandler toLowerSampleMessageHandler = new ToLowerSampleMessageHandler("ToLower");
      IncreaseMessageHandler increaseMessageHandler = new IncreaseMessageHandler(0);

      IMessageBus messageBus = new InMemoryMessageBus();
      messageBus.Subscribe<SampleMessageHandler, SampleMessage>(sampleMessageHandler);
      messageBus.Subscribe<ToLowerSampleMessageHandler, SampleMessage>(toLowerSampleMessageHandler);
      messageBus.Subscribe<IncreaseMessageHandler, IncreaseMessage>(increaseMessageHandler);

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

    public SampleMessageHandler(string myMessage)
    {
      MyMessage = myMessage;
    }

    public async Task Handle(SampleMessage message)
    {
      MyMessage = message.Message;

      await Task.FromResult(0);
    }
  }

  public class ToLowerSampleMessageHandler : IMessageHandler<SampleMessage>
  {
    public string MyMessage { get; set; }

    public ToLowerSampleMessageHandler(string myMessage)
    {
      MyMessage = myMessage;
    }

    public async Task Handle(SampleMessage message)
    {
      MyMessage = message.Message.ToLower();

      await Task.FromResult(0);
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

    public IncreaseMessageHandler(int initialValue)
    {
      Value = initialValue;
    }

    public async Task Handle(IncreaseMessage message)
    {
      Value += message.Increase;

      await Task.FromResult(0);
    }
  }
}
