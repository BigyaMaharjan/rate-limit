using RateLimit.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.TextTemplating;

namespace RateLimit.TextTemplating;
public class TemplateDemo(
    IRepository<Template, Guid> templateRepository,
    ITemplateRenderer templateRenderer,
    AsyncQueryableExecuter asyncQueryableExecuter) : ITransientDependency
{
    public async Task RunAsync()
    {
        var queryable = await templateRepository.GetQueryableAsync();

        // Use asyncQueryableExecuter to fetch the first Templates string
        var templateString = await asyncQueryableExecuter.FirstOrDefaultAsync(
            queryable.Select(t => t.Templates)
        );

        var context = new Dictionary<string, object>
        {
            ["item"] = "Apple"
        };

        var rendered = await templateRenderer.RenderAsync(
            templateString,
            context
        );
    }
}
