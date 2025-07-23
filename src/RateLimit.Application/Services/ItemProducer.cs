using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RateLimit.ETOs;
using RateLimit.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;

namespace RateLimit.Services;
public class ItemProducer : ApplicationService, IItemProducer
{
    private readonly ILogger<ItemProducer> _logger;
    private readonly IModel _channel;

    public ItemProducer(
        ILogger<ItemProducer> logger,
        IModel channel)
    {
        _logger = logger;
        _channel = channel;

        // Declare exchange (could be moved to a separate setup method)
        _channel.ExchangeDeclare(exchange: "item-exchange", type: ExchangeType.Direct, durable: true);
    }
    public async Task PublishItemAsync(CreateItemEto eventData)
    {
        try
        {
            var message = System.Text.Json.JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "item-exchange",
                routingKey: "item-queue",
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published item: {ItemData}", message);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "::ItemService:: - CreateItemAsync - :: Exception::");
            throw new UserFriendlyException(ex.Message);
        }
    }
}