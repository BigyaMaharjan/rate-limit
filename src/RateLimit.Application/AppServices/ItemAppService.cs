using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using RateLimit.CacheKeys;
using RateLimit.Entities;
using RateLimit.Interfaces;
using RateLimit.Interfaces.Dtos;
using RateLimit.MessageCodes;
using RateLimit.ResponseModels;
using RateLimit.Validators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;

namespace RateLimit.AppServices;

public class ItemAppService(
    IRepository<Item, Guid> itemRepository,
    ILogger<ItemAppService> logger,
    IDistributedCache<ItemDto> cache,
    IObjectMapper objectMapper,
    IConfiguration configuration,
    IGuidGenerator guidGenerator) : ApplicationService, IItemAppService
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

            var cacheKey = RateLimitCacheKey.ITEM_CACHE_KEY;

            var cachedItem = await cache.GetOrAddAsync(cacheKey,
                async () =>
                {
                    logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
                    var itemQueryable = await itemRepository.GetQueryableAsync();
                    var item = itemQueryable.FirstOrDefault(c => c.Id == id);

                    if (item == null)
                    {
                        logger.LogWarning("ItemAppService - GetAsync failed: {Message}", ItemMessageCodes.ItemNotFound);
                        throw new UserFriendlyException(ItemMessageCodes.ItemNotFound, "400");
                    }

                    return objectMapper.Map<Item, ItemDto>(item);
                }
            );

            var response = new ResponseModel<ItemDto>
            {
                Success = true,
                Message = ItemMessageCodes.ItemRetrieved,
                Data = cachedItem
            };

            logger.LogDebug("ItemAppService - GetAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - GetAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - GetAsync: Internal server error in GetAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }
    }

    public async Task<ResponseModel<List<ItemDto>>> GetAllAsync()
    {
        try
        {
            logger.LogInformation("ItemAppService - GetAllAsync called }");

            var items = await itemRepository.GetListAsync();
            if (items == null || !items.Any())
            {
                logger.LogWarning("ItemAppService - GetAllAsync failed: {Message}", ItemMessageCodes.ItemNotFound);
                throw new UserFriendlyException(ItemMessageCodes.ItemNotFound, "400");
            }

            var response = new ResponseModel<List<ItemDto>>
            {
                Success = true,
                Message = ItemMessageCodes.ItemRetrieved,
                Data = objectMapper.Map<List<Item>, List<ItemDto>>(items)
            };

            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - GetAllAsync: Internal server error in GetAsync.");
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
            //Concurrency Control
            var retryPolicy = Policy
                .Handle<AbpDbConcurrencyException>()
                .WaitAndRetryAsync(retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
                onRetry: (ex, delay) =>
                {
                    logger.LogWarning("Delay {RetryCount} due to concurrency exception: {Message}", delay, ex.Message);
                });

            await retryPolicy.ExecuteAsync(async () =>
            {
                using (var uow = UnitOfWorkManager.Begin(requiresNew: true))
                {
                    var item = await itemRepository.GetAsync(id);
                    objectMapper.Map(input, item);
                    logger.LogInformation("ItemAppService - UpdateAsync with item: {@Input}", item);
                    await itemRepository.UpdateAsync(item);

                    await uow.SaveChangesAsync();
                }
            });

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

    public async Task<ResponseModel> UploadFileAsync(IFormFile file)
    {
        try
        {
            logger.LogInformation("ItemAppService - UploadFileAsync with File: {@file}", file);

            var allowedExtensions = configuration.GetSection("FileUpload:AllowedTypes").Get<string[]>() ?? Array.Empty<string>();
            var maxSize = configuration.GetValue<long>("FileUpload:MaxSize");

            if (file == null || file.Length == 0)
            {
                throw new UserFriendlyException("No file uploaded.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new UserFriendlyException($"File type '{extension}' is not allowed.");
            }

            if (file.Length > maxSize)
            {
                throw new UserFriendlyException($"File size exceeds the maximum allowed size of 5MB.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{guidGenerator.Create()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var response = new ResponseModel
            {
                Success = true,
                Message = ItemMessageCodes.ItemUpdated
            };

            logger.LogDebug("ItemAppService - UploadFileAsync response: {@Response}", response);
            logger.LogInformation("ItemAppService - UploadFileAsync completed successfully.");
            return response;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in UploadFileAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }
    }

    [HttpGet]
    public async Task<FileResult> DownloadFileAsync(string fileName)
    {
        try
        {
            logger.LogInformation("ItemAppService - DownloadFileAsync with fileName: {fileName}", fileName);

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                throw new UserFriendlyException("File not found.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;
            var contentType = GetContentType(filePath);

            logger.LogInformation("ItemAppService - DownloadFileAsync completed successfully.");
            return new FileStreamResult(memory, contentType)
            {
                FileDownloadName = fileName
            };
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ItemAppService - Internal server error in DownloadFileAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }
    }

    private string GetContentType(string path)
    {
        var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
        { ".zip", "application/zip" },
        { ".rar", "application/x-rar-compressed" }
    };

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return types.TryGetValue(ext, out string contentType) ? contentType : "application/octet-stream";
    }

   
}