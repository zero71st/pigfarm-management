using PigFarmManagement.Server.Infrastructure.Data;
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
        // Infrastructure
        services.AddSingleton<InMemoryDataStore>();

        // Customer Feature
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerService, CustomerService>();

        // PigPen Feature
        services.AddScoped<IPigPenRepository, PigPenRepository>();
        services.AddScoped<IPigPenService, PigPenService>();

        // Feed Feature
        services.AddScoped<IFeedRepository, FeedRepository>();
        services.AddScoped<IFeedService, FeedService>();

        // Feed Import Feature
        services.AddScoped<IFeedImportService, FeedImportService>();

        // Dashboard Feature
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
