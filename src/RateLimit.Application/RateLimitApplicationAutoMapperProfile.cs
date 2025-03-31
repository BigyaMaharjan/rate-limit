using AutoMapper;
using RateLimit.Entities;
using RateLimit.Interfaces.Dtos;

namespace RateLimit;

public class RateLimitApplicationAutoMapperProfile : Profile
{
    public RateLimitApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<Item, ItemDto>();
        CreateMap<CreateUpdateItemDto, Item>();
    }
}
