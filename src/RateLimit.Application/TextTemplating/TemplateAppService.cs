using Microsoft.Extensions.Logging;
using RateLimit.Entities;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.TextTemplating;

namespace RateLimit.TextTemplating;

public class TemplateAppService(
    IRepository<Template, Guid> templateRepository,
    ITemplateRenderer templateRenderer,
    ILogger<TemplateAppService> logger) : ApplicationService
{
    public async Task<string> RenderTemplateFromDbAsync()
    {
        try
        {
            logger.LogInformation("TemplateAppService - RenderTemplateFromDbAsync started:");

            var templateString = await GetTemplateFromDbAsync();

            var scribanTemplate = Scriban.Template.Parse(templateString);

            if (scribanTemplate.HasErrors)
            {
                throw new Exception("Template parsing failed: " + string.Join(", ", scribanTemplate.Messages.Select(m => m.Message)));
            }

            var context = new { item = "Apple" }; // You can use anonymous or dictionary

            var rendered = await Task.FromResult(scribanTemplate.Render(context));

            logger.LogDebug("TemplateAppService - RenderTemplateFromDbAsync response: {@Response}", rendered);
            logger.LogInformation("TemplateAppService - RenderTemplateFromDbAsync completed successfully.");
            return rendered;
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TemplateAppService - Internal server error in RenderTemplateFromDbAsync.");
            throw new UserFriendlyException(RateLimitDomainErrorCodes.InternalServerError, "500");
        }
    }

    private async Task<string> GetTemplateFromDbAsync()
    {
        var queryable = await templateRepository.GetQueryableAsync();

        var templateString = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Select(t => t.Templates)
        );

        if (templateString == null)
        {
            logger.LogWarning("TemplateAppService - GetTemplateFromDbAsync failed: {Message}", "Template not found.");
            throw new UserFriendlyException("Template not found.", "400");
        }

        return templateString;
    }
}