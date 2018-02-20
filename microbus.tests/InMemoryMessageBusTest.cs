using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using tomware.Microbus.Core;

namespace microbus.tests
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

    public void Handle(SampleMessage message)
    {
      MyMessage = message.Message;
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

    public void Handle(IncreaseMessage message)
    {
      Value += message.Increase;
    }
  }
}
