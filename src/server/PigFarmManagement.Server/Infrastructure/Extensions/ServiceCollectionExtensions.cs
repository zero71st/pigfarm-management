using PigFarmManagement.Server.Infrastructure.Data.Repositories;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Server.Features.FeedProgress;
using PigFarmManagement.Server.Features.Dashboard;
using PigFarmManagement.Server.Services;
using PigFarmManagement.Server.Services.ExternalServices;
using PigFarmManagement.Shared.Features.Feeds.Contracts;

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
        services.AddScoped<Infrastructure.Data.Repositories.IFeedFormulaRepository, Infrastructure.Data.Repositories.FeedFormulaRepository>();

        // Services
        services.AddScoped<Features.Customers.ICustomerService, Features.Customers.CustomerService>();
        services.AddScoped<Features.Customers.ICustomerLocationService, Features.Customers.CustomerLocationService>();
        services.AddScoped<Features.Customers.ICustomerDeletionService, Features.Customers.CustomerDeletionService>();
        services.AddScoped<IPigPenService, PigPenService>();
        services.AddScoped<IPigPenDetailService, PigPenDetailService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IFeedFormulaService, FeedFormulaService>();
        services.AddScoped<IFeedImportService, FeedImportService>();
        services.AddScoped<IFeedProgressService, FeedProgressService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<Features.Products.IProductService, Features.Products.ProductService>();
        services.AddScoped<FormulaMigrationService, FormulaMigrationService>();

        // POSPOS feed client (used to fetch transactions)
        services.AddHttpClient<IPosposTransactionClient, PosposTransactionClient>();

        return services;
    }
}
