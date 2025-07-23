using System;

namespace RateLimit.Interfaces.Dtos;
public class IsolationTestInput
{
    public Guid ItemId { get; set; }
    public int DelayMs { get; set; }
}