using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace RateLimit.Entities;
public class Item : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}