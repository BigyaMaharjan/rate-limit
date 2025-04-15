using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RateLimit.Interfaces;
using RateLimit.Services;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace RateLimit.RabbitMQ;

public class RabbitMQModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.AddSingleton<IConnectionFactory>(sp =>
        {
            return new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Connections:Default:HostName"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Connections:Default:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Connections:Default:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Connections:Default:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };
        });

        context.Services.AddSingleton<IConnection>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            return factory.CreateConnection();
        });

        context.Services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            return connection.CreateModel();
        });

        context.Services.AddTransient<IItemProducer, ItemProducer>();
    }
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var connection = context.ServiceProvider.GetService<IConnection>();
        connection?.Dispose();
    }
}