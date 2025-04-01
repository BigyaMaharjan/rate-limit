using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RateLimit.ETOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RateLimit.Services;

public class ItemConsumer : IDisposable
{
    private readonly IModel _channel;
    private readonly ILogger<ItemConsumer> _logger;

    public ItemConsumer(IModel channel, ILogger<ItemConsumer> logger)
    {
        _channel = channel;
        _logger = logger;
        ConfigureQueue();
        StartConsuming();
    }

    private void ConfigureQueue()
    {
        _channel.ExchangeDeclare(exchange: "item-exchange", type: ExchangeType.Direct, durable: true);

        // Declare DLQ exchange and queue
        _channel.ExchangeDeclare(exchange: "dlx-exchange", type: ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(queue: "dlq-queue", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: "dlq-queue", exchange: "dlx-exchange", routingKey: "dlq-routing-key");

        // Declare main queue with DLQ settings
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "dlx-exchange" },
            { "x-dead-letter-routing-key", "dlq-routing-key" }
        };

        _channel.QueueDeclare(queue: "item-queue", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

        _channel.QueueBind(queue: "item-queue", exchange: "item-exchange", routingKey: "item-queue");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    private void StartConsuming()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventData = System.Text.Json.JsonSerializer.Deserialize<CreateItemEto>(message);

                // Simulate message processing (replace with your logic)
                _logger.LogInformation("Processing item: {ItemId}", eventData.Id);
                await ProcessMessageAsync(eventData);

                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                var retryCount = GetRetryCount(ea.BasicProperties) + 1;
                if (retryCount < 3) // Max 3 retries
                {
                    var properties = _channel.CreateBasicProperties();
                    properties.Headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>();
                    properties.Headers["retry-count"] = retryCount;

                    // Requeue with updated retry count
                    _channel.BasicPublish(
                        exchange: "item-exchange",
                        routingKey: "item-queue",
                        basicProperties: properties,
                        body: ea.Body);

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false); // Acknowledge original message
                }
                else
                {
                    _logger.LogWarning("Max retries reached for message");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false); // Drop or send to DLQ
                }
            }
        };

        _channel.BasicConsume(queue: "item-queue", autoAck: false, consumer: consumer);
    }

    private int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers != null && properties.Headers.TryGetValue("retry-count", out var value))
        {
            return Convert.ToInt32(value);
        }
        return 0;
    }

    private async Task ProcessMessageAsync(CreateItemEto eventData)
    {
        // Simulate async work (e.g., save to DB, call an API)
        await Task.Delay(1000); // Replace with actual processing logic
        _logger.LogInformation("Item {ItemId} processed successfully", eventData.Id);
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}