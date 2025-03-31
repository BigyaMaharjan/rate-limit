using Microsoft.Extensions.Logging;
using RateLimit.Entities;
using RateLimit.Interfaces;
using RateLimit.Interfaces.Dtos;
using RateLimit.MessageCodes;
using RateLimit.ResponseModels;
using RateLimit.Validators;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace RateLimit.AppServices;
public class ItemAppService(
    IRepository<Item, Guid> itemRepository,
    ILogger<ItemAppService> logger,
    IObjectMapper objectMapper) : ApplicationService, IItemAppService
{
    public async Task<ResponseModel<CreateItemResponseDto>> CreateAsync(CreateUpdateItemDto input)
    {
        try
        {
            logger.LogDebug("ItemAppService - CreateAsync with input: {@Input}", input);

            var validator = new CreateUpdateItemDtoValidator();
            var validationResult = await validator.ValidateAsync(input);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                logger.LogWarning("Validation failed: {Errors}", errors);
                throw new UserFriendlyException(errors, "400");
            }

            input = input with { Name = input.Name.Trim() };

            var item = objectMapper.Map<CreateUpdateItemDto, Item>(input);

            await itemRepository.InsertAsync(item);

            var response = new ResponseModel<CreateItemResponseDto>
            {
                Success = true,
                Message = ItemMessageCodes.ItemCreated,
                Data = new CreateItemResponseDto
                {
                    Id = item.Id,
                }
            };

            logger.LogDebug("ItemAppService - CreateAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - CreateAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in CreateAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }

    }

    public async Task<ResponseModel> DeleteAsync(Guid id)
    {
        try
        {
            logger.LogInformation("ItemAppService - DeleteAsync called with Id: {Id}", id);

            var itemExists = await itemRepository.AnyAsync(t => t.Id == id);
            if (!itemExists)
            {
                logger.LogWarning("ItemAppService - DeleteAsync failed: {Message}", ItemMessageCodes.ItemNotFound);
                throw new UserFriendlyException(ItemMessageCodes.ItemNotFound, "400");
            }

            await itemRepository.DeleteAsync(id);

            var response = new ResponseModel
            {
                Success = true,
                Message = ItemMessageCodes.ItemDeleted
            };

            logger.LogDebug("ItemAppService - DeleteAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - DeleteAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in DeleteAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }

    }

    public async Task<ResponseModel<ItemDto>> GetAsync(Guid id)
    {
        try
        {
            logger.LogInformation("ItemAppService - GetAsync called with Id: {Id}", id);

            var itemQueryable = await itemRepository.GetQueryableAsync();
            var item = itemQueryable.Where(c => c.Id == id).FirstOrDefault();

            if (item == null)
            {
                logger.LogWarning("ItemAppService - GetAsync failed: {Message}", ItemMessageCodes.ItemNotFound);
                throw new UserFriendlyException(ItemMessageCodes.ItemNotFound, "400");
            }

            var itemDto = objectMapper.Map<Item, ItemDto>(item);

            var response = new ResponseModel<ItemDto>
            {
                Success = true,
                Message = ItemMessageCodes.ItemCreated,
                Data = itemDto
            };

            logger.LogDebug("ItemAppService - GetAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - DeleteAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in DeleteAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }

    }

    public async Task<ResponseModel> UpdateAsync(Guid id, CreateUpdateItemDto input)
    {
        try
        {
            logger.LogInformation("ItemAppService - UpdateAsync with input: {@Input}", input);

            var validator = new CreateUpdateItemDtoValidator();
            var validationResult = await validator.ValidateAsync(input);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                logger.LogWarning("Validation failed: {Errors}", errors);
                throw new UserFriendlyException(errors, "400");
            }

            input = input with { Name = input.Name.Trim() };

            var item = objectMapper.Map<CreateUpdateItemDto, Item>(input);

            await itemRepository.UpdateAsync(item);

            var response = new ResponseModel
            {
                Success = true,
                Message = ItemMessageCodes.ItemUpdated
            };

            logger.LogDebug("ItemAppService - UpdateAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - UpdateAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in UpdateAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }
    }
}
