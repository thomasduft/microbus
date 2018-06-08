using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace RabbitMQ.MessageBus
{
  public interface IRabbitMQPersistentConnection : IDisposable
  {
    bool IsConnected { get; }

    bool TryConnect();

    IModel CreateModel();
  }

  public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
  {
    private readonly string _clientName;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
    private readonly int _retryCount;
    IConnection _connection;
    bool _disposed;

    object sync_root = new object();

    public DefaultRabbitMQPersistentConnection(
      ILogger<DefaultRabbitMQPersistentConnection> logger,
      IRabbitMQMessageBusConfiguration rabbitMQMessageBusConfiguration
    )
    {
      _connectionFactory = DefaultRabbitMQPersistentConnection
        .CreateConnectionFactory(rabbitMQMessageBusConfiguration.ConnectionString);
      _clientName = rabbitMQMessageBusConfiguration.ClientName ?? "pleaseAddClientName";
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _retryCount = rabbitMQMessageBusConfiguration.RetryCount;
    }

    public bool IsConnected
    {
      get
      {
        return _connection != null && _connection.IsOpen && !_disposed;
      }
    }

    public IModel CreateModel()
    {
      if (!IsConnected)
      {
        throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
      }

      return _connection.CreateModel();
    }

    public void Dispose()
    {
      if (_disposed) return;

      _disposed = true;

      try
      {
        _connection.Dispose();
      }
      catch (IOException ex)
      {
        _logger.LogCritical(ex.ToString());
      }
    }

    public bool TryConnect()
    {
      _logger.LogInformation("RabbitMQ Client is trying to connect");

      lock (sync_root)
      {
        var policy = Policy
            .Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(
              _retryCount,
              retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
              {
                _logger.LogWarning(ex.ToString());
              }
        );

        policy.Execute(() =>
        {
          _connection = _connectionFactory.CreateConnection(_clientName);
        });

        if (IsConnected)
        {
          _connection.ConnectionShutdown += OnConnectionShutdown;
          _connection.CallbackException += OnCallbackException;
          _connection.ConnectionBlocked += OnConnectionBlocked;

          _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");

          return true;
        }
        else
        {
          _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

          return false;
        }
      }
    }

    private static IConnectionFactory CreateConnectionFactory(string connection)
    {
      var connectionString = new ConnectionString(connection);

      ConnectionFactory factory = new ConnectionFactory
      {
        UserName = connectionString.GetValueFromPart("username"),
        Password = connectionString.GetValueFromPart("password"),
        HostName = connectionString.GetValueFromPart("host")
        // VirtualHost = connectionString.GetValueFromPart("vhost")
      };

      return factory;
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
      if (_disposed) return;

      _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

      TryConnect();
    }

    private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
      if (_disposed) return;

      _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

      TryConnect();
    }

    private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
      if (_disposed) return;

      _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

      TryConnect();
    }

    private class ConnectionString
    {
      // host=localhost:5672;username=guest;password=guest
      private readonly string _connection;
      private readonly string[] _parts;
      public ConnectionString(string connection)
      {
        _connection = connection;
        _parts = _connection.Split(';');
      }

      public bool HasPart(string part)
      {
        return _parts.Any(p => p.ToLowerInvariant().StartsWith(part.ToLowerInvariant()));
      }

      public string GetValueFromPart(string part)
      {
        if (!HasPart(part)) return string.Empty;

        var pair = _parts.First(p => p.ToLowerInvariant().StartsWith(part.ToLowerInvariant()));
        var value = pair.Split('=')[1];
        return value;
      }
    }
  }
}
