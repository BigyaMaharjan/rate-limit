using Microsoft.Extensions.Logging;
using RateLimit.ETOs;
using RateLimit.Interfaces;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace RateLimit.EventHandlers;

public class CreateItemEventHandler(
    ILogger<CreateItemEventHandler> logger,
    IItemProducer itemService) : IDistributedEventHandler<CreateItemEto>, ITransientDependency
{
    public async Task HandleEventAsync(CreateItemEto eventData)
    {
		try
		{
            logger.LogInformation($"Order Received: {eventData.Id}, Customer: {eventData.Name}, Amount: {eventData.Price}");
            await itemService.PublishItemAsync(eventData);
            return;
        }
		catch (Exception ex)
		{
            logger.LogInformation(ex, "::CreateItemEventHandler:: - HandleEventAsync - ::Exception::");
            throw new UserFriendlyException(ex.Message);
		}
    }
}