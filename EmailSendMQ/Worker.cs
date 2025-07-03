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
        private readonly IMongoCollection<EmailModelMongoDB> _collection;
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;

            var mongoClient = new MongoClient(config["MongoConnection"]);
            var mongoDb = mongoClient.GetDatabase("EmailDB");
            _collection = mongoDb.GetCollection<EmailModelMongoDB>("EmailQueue");

            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"],
                UserName = config["RabbitMQ:User"],
                Password = config["RabbitMQ:Password"],
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
            _logger.LogInformation("Worker started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var email = await _collection.Find(_ => true)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (email != null)
                    {
                        var json = JsonSerializer.Serialize(email);
                        var body = Encoding.UTF8.GetBytes(json);

                        _channel.BasicPublish(
                            exchange: "",
                            routingKey: "email_queue",
                            basicProperties: null,
                            body: body);

                        await _collection.DeleteOneAsync(
                            e => e.Id == email.Id,
                            stoppingToken);

                        _logger.LogInformation(
                            $"[✓] Email from {email.Email} sent to queue.");
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processing error.");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}