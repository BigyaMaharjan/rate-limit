using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RateLimit.Interfaces.Dtos;
using RateLimit.ResponseModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace RateLimit.Interfaces;

public interface IItemAppService : IApplicationService
{
    Task<ResponseModel<ItemDto>> GetAsync(Guid id);

    Task<ResponseModel<List<ItemDto>>> GetAllAsync();

    Task<ResponseModel<CreateItemResponseDto>> CreateAsync(CreateUpdateItemDto input);

    Task<ResponseModel> UpdateAsync(Guid id, CreateUpdateItemDto input);

    Task<ResponseModel> DeleteAsync(Guid id);

    Task<ResponseModel> UploadFileAsync(IFormFile file);

    Task<FileResult> DownloadFileAsync(string fileName);
}