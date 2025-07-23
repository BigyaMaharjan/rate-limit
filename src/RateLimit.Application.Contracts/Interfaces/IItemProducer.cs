using RateLimit.ETOs;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace RateLimit.Interfaces;
public interface IItemProducer : IApplicationService
{
    Task PublishItemAsync(CreateItemEto eventData);
}