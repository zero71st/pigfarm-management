using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.Dashboard;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Shared.Services;

namespace PigFarmManagement.Server.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // EF Core Repositories
        services.AddScoped<Infrastructure.Data.Repositories.ICustomerRepository, Infrastructure.Data.Repositories.CustomerRepository>();
        services.AddScoped<Infrastructure.Data.Repositories.IPigPenRepository, Infrastructure.Data.Repositories.PigPenRepository>();
        services.AddScoped<Infrastructure.Data.Repositories.IFeedRepository, Infrastructure.Data.Repositories.FeedRepository>();
        services.AddScoped<Infrastructure.Data.Repositories.IDepositRepository, Infrastructure.Data.Repositories.DepositRepository>();
        services.AddScoped<Infrastructure.Data.Repositories.IHarvestRepository, Infrastructure.Data.Repositories.HarvestRepository>();

        // Legacy Feature Repositories (for backward compatibility)
        services.AddScoped<Features.Customers.ICustomerRepository, Features.Customers.CustomerRepository>();
        services.AddScoped<Features.PigPens.IPigPenRepository, Features.PigPens.PigPenRepository>();
        services.AddScoped<Features.Feeds.IFeedRepository, Features.Feeds.FeedRepository>();

        // Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPigPenService, PigPenService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IFeedImportService, FeedImportService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
