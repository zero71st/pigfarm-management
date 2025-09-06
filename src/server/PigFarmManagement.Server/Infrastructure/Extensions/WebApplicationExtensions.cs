using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.Dashboard;

namespace PigFarmManagement.Server.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        // Map all feature endpoints
        app.MapCustomerEndpoints();
        app.MapPigPenEndpoints();
        app.MapFeedEndpoints();
        app.MapDashboardEndpoints();

        return app;
    }
}
