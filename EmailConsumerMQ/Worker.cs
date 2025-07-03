using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using EmailConsumerMQ.Model.MongoDB;
using EmailConsumerMQ.Data;
using EmailConsumerMQ.Model;

namespace EmailConsumerMQ
{

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IConfiguration config, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Host"],
                UserName = config["RabbitMQ:User"],
                Password = config["RabbitMQ:Password"],
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "email_queue",
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailConsumerMQ iniciado e consumindo fila...");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                EmailModelMongoDB email;
                try
                {
                    email = JsonSerializer.Deserialize<EmailModelMongoDB>(json) ?? throw new Exception("Payload inválido");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao desserializar mensagem");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                bool success = false;
                string? error = null;

                try
                {
                    await SendEmailAsync(email);
                    success = true;
                    _logger.LogInformation($"E-mail enviado para {email.Email}");
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    _logger.LogError(ex, $"Erro ao enviar e-mail para {email.Email}");
                }

                // Salvar log no SQL Server
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var log = new EmailLog
                    {
                        Email = email.Email,
                        Subject = email.Subject,
                        Success = success,
                        Error = error,
                        SentAt = DateTime.UtcNow
                    };

                    db.EmailLogs.Add(log);
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar log no banco de dados");
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: "email_queue", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task SendEmailAsync(EmailModelMongoDB email)
        {
            using var smtp = new SmtpClient("smtp.seudominio.com")
            {
                Credentials = new NetworkCredential("usuario", "senha"),
                EnableSsl = true,
                Port = 587
            };

            var mail = new MailMessage("teste.vasc@gmail.com", email.Email)
            {
                Subject = email.Subject,
                Body = email.Message,
                IsBodyHtml = false
            };

            await smtp.SendMailAsync(mail);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            base.Dispose();
        }
    }

}
