using RateLimit.Interfaces.Dtos;
using RateLimit.ResponseModels;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace RateLimit.Interfaces;
public interface IItemAppService : IApplicationService
{
    Task<ResponseModel<ItemDto>> GetAsync(Guid id);

    Task<ResponseModel<CreateItemResponseDto>> CreateAsync(CreateUpdateItemDto input);

    Task<ResponseModel> UpdateAsync(Guid id, CreateUpdateItemDto input);

    Task<ResponseModel> DeleteAsync(Guid id);

}
