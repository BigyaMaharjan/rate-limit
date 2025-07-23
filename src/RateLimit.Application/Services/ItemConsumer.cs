using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RateLimit.ETOs;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace RateLimit.Services
{
    public class ItemConsumer : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly ILogger<ItemConsumer> _logger;
        private readonly IModel _channel;

        public ItemConsumer(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ItemConsumer> logger,
            IModel channel)
            : base(timer, serviceScopeFactory)
        {
            _logger = logger;
            _channel = channel;

            Timer.Period = 1000; // Optional, not needed for RabbitMQ consuming
        }

        private void ConfigureQueue()
        {
            _channel.ExchangeDeclare(
                exchange: "item-exchange",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false
            );

            _channel.QueueDeclare(
                queue: "item-queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            _channel.QueueBind(
                queue: "item-queue",
                exchange: "item-exchange",
                routingKey: "item-queue"
            );
            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false
            );
        }

        private void StartConsuming(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("🚫 Cancellation requested, ignoring message.");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var eventData = JsonSerializer.Deserialize<CreateItemEto>(message);

                    _logger.LogInformation("🔥 Consumed item: {ItemId} - {ItemName}", eventData.Id, eventData.Name);
                    await ProcessMessageAsync(eventData, cancellationToken);

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Oops, failed to process message!");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: "item-queue",
                autoAck: false,
                consumer: consumer
            );
            _logger.LogInformation("👂 Consumer is now listening to item-queue...");
        }

        private async Task ProcessMessageAsync(CreateItemEto eventData, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken); // Simulate work, respect cancellation
            _logger.LogInformation("✅ Item {ItemId} processed like a boss!", eventData.Id);
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCancellationToken = cancellationToken;

            ConfigureQueue();
            StartConsuming(cancellationToken);

            await base.StartAsync(cancellationToken);
            Timer.Start(cancellationToken); // Optional
        }

        public override async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Timer.Stop(cancellationToken);
            await base.StopAsync(cancellationToken);

            _channel?.Close();
            _logger.LogInformation("🛑 Consumer shut down.");
        }

        protected override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            return Task.CompletedTask; // No periodic work needed
        }
    }
}