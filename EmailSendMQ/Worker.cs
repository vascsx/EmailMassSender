using EmailSendMQ.Model.Mongo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace EmailSendMQ
{
    public class Worker : BackgroundService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private IMongoCollection<EmailModelMongoDB> _collection = default!;
        private IModel _channel = default!;
        private IConnection _connection = default!;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            ConfigureMongo();
            ConfigureRabbitMQ();
        }

        private void ConfigureMongo()
        {
            var mongoClient = new MongoClient(_config["MongoConnection"]);
            var mongoDatabase = mongoClient.GetDatabase("EmailDB");
            _collection = mongoDatabase.GetCollection<EmailModelMongoDB>("EmailQueue");
        }

        private void ConfigureRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"],
                Port = int.TryParse(_config["RabbitMQ:Port"], out int port) ? port : 5672,
                UserName = _config["RabbitMQ:User"],
                Password = _config["RabbitMQ:Password"],
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "email_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Worker iniciado...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var email = await _collection.Find(_ => true)
                                                 .FirstOrDefaultAsync(stoppingToken);

                    if (email != null)
                    {
                        SendToQueue(email);
                        await RemoveFromMongo(email, stoppingToken);

                        _logger.LogInformation($"[✓] Email de {email.Email} enviado para fila.");
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante o processamento.");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        private void SendToQueue(EmailModelMongoDB email)
        {
            var json = JsonSerializer.Serialize(email);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "email_queue",
                basicProperties: null,
                body: body);
        }

        private async Task RemoveFromMongo(EmailModelMongoDB email, CancellationToken token)
        {
            await _collection.DeleteOneAsync(e => e.Id == email.Id, token);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
