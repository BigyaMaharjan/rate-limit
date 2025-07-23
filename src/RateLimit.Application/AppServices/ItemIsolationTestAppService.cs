using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RateLimit.Entities;
using RateLimit.Interfaces.Dtos;
using System;
using System.Data;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace RateLimit.AppServices;

public class ItemIsolationTestAppService(
    IUnitOfWorkManager unitOfWorkManager,
    IRepository<Item, Guid> itemRepository,
    ILogger<ItemIsolationTestAppService> logger) : ApplicationService
{
    [HttpPost("read-uncommitted")]
    public async Task<IsolationTestResultDto> ReadUncommitted(IsolationTestInput input)
    {
        return await RunIsolationLevel(input, IsolationLevel.ReadUncommitted);
    }

    [HttpPost("read-committed")]
    public async Task<IsolationTestResultDto> ReadCommitted(IsolationTestInput input)
    {
        return await RunIsolationLevel(input, IsolationLevel.ReadCommitted);
    }

    [HttpPost("repeatable-read")]
    public async Task<IsolationTestResultDto> RepeatableRead(IsolationTestInput input)
    {
        return await RunIsolationLevel(input, IsolationLevel.RepeatableRead);
    }

    [HttpPost("serializable")]
    public async Task<IsolationTestResultDto> Serializable(IsolationTestInput input)
    {
        return await RunIsolationLevel(input, IsolationLevel.Serializable);
    }

    [HttpPost("snapshot")]
    public async Task<IsolationTestResultDto> Snapshot(IsolationTestInput input)
    {
        return await RunIsolationLevel(input, IsolationLevel.Snapshot);
    }

    private async Task<IsolationTestResultDto> RunIsolationLevel(IsolationTestInput input, IsolationLevel level)
    {
        using (var uow = unitOfWorkManager.Begin(isTransactional: true, isolationLevel: level, timeout: 3000))
        {
            var itemBefore = await itemRepository.GetAsync(input.ItemId);
            logger.LogInformation("Before delay - Item Name: {Name}", itemBefore.Name);

            await Task.Delay(input.DelayMs);

            var itemAfter = await itemRepository.GetAsync(input.ItemId);
            logger.LogInformation("After delay - Item Name: {Name}", itemAfter.Name);

            await uow.CompleteAsync();

            return new IsolationTestResultDto
            {
                IsolationLevel = level.ToString(),
                NameBefore = itemBefore.Name,
                NameAfter = itemAfter.Name,
                Changed = itemBefore.Name != itemAfter.Name
            };
        }
    }
}