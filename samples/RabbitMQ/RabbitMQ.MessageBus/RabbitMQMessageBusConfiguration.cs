namespace RabbitMQ.MessageBus
{
  public class RabbitMQMessageBusConfiguration
  {
    public string ClientName { get; set; }
    public string QueueName { get; set; }
    public string BrokerName { get; set; }
    public string BrokerStrategy { get; set; }
    public string ConnectionString { get; set; }
    public int RetryCount { get; set; }
    public bool ConfirmSelect { get; set; }
  }
}