namespace RabbitMQ.MessageBus
{
  public interface IRabbitMQMessageBusConfiguration
  {
    string ClientName { get; }

    string ConnectionString { get; }
    
    int RetryCount { get; }
  }

  public class RabbitMQMessageBusConfiguration : IRabbitMQMessageBusConfiguration
  {
    private readonly string _clientName;
    private readonly string _connectionString;
    private readonly int _retryCount;

    public RabbitMQMessageBusConfiguration(
      string clientName,
      string connectionString,
      int retryCount
    ) {
      this._clientName = clientName;
      this._connectionString = connectionString;
      this._retryCount = retryCount;
    }

    public string ClientName => this._clientName;
    public string ConnectionString => this._connectionString;
    public int RetryCount => this._retryCount;

  }
}