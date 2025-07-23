using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace RateLimit.Entities;

public class Template : FullAuditedAggregateRoot<Guid>
{
    public string SystemName { get; set; }
    public string DisplayName { get; set; }
    public string Templates { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}