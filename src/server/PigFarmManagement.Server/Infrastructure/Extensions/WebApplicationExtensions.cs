using PigFarmManagement.Server.Features.Customers;
using PigFarmManagement.Server.Features.PigPens;
using PigFarmManagement.Server.Features.Feeds;
using PigFarmManagement.Server.Features.FeedFormulas;
using PigFarmManagement.Server.Features.FeedProgress;
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
        app.MapFeedFormulaEndpoints();
        app.MapFeedFormulaCalculationEndpoints();
        app.MapFeedImportEndpoints();
        app.MapFeedProgressEndpoints();
        app.MapDashboardEndpoints();

        return app;
    }
}
