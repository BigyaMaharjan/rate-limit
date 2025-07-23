namespace RateLimit.Interfaces.Dtos;
public record CreateUpdateItemDto
{
    public string Name { get; init; }
    public decimal Price { get; init; }
    //public string? ConcurrencyStamp { get; init; }
}
