namespace PigFarmManagement.Server.Infrastructure.Settings;

/// <summary>
/// Configuration settings for Google Maps integration
/// </summary>
public class GoogleMapsOptions
{
    public const string SectionName = "GoogleMaps";
    
    public string ApiKey { get; set; } = string.Empty;
    public int DefaultZoom { get; set; } = 10;
    public GoogleMapsDefaultCenter DefaultCenter { get; set; } = new();
}

public class GoogleMapsDefaultCenter
{
    public decimal Latitude { get; set; } = 37.7749m; // San Francisco default
    public decimal Longitude { get; set; } = -122.4194m;
}