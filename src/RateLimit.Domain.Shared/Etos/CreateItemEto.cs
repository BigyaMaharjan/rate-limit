using System;
using Volo.Abp.EventBus;

namespace RateLimit.ETOs;

[EventName("RateLimit.Item.Create")]
public record CreateItemEto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
}