using Microsoft.JSInterop;
using PigFarmManagement.Shared.Models;

namespace PigFarmManagement.Client.Services;

/// <summary>
/// Service for Google Maps integration using JavaScript interop
/// </summary>
public interface IGoogleMapsService
{
    Task<bool> IsApiLoadedAsync();
    Task<bool> InitializeMapAsync(string elementId, decimal latitude, decimal longitude, int zoom = 10);
    Task<GoogleMapMarker?> AddMarkerAsync(string elementId, decimal latitude, decimal longitude, string? title = null, bool draggable = false);
    Task ClearMarkersAsync(string elementId);
    Task<bool> UpdateMarkerAsync(string elementId, int markerId, decimal latitude, decimal longitude);
    Task<bool> SetupClickListenerAsync(string elementId);
    Task<bool> SetupMarkerDragListenerAsync(string elementId, int markerId);
    Task<bool> CenterMapAsync(string elementId, decimal latitude, decimal longitude, int? zoom = null);
    Task<GoogleMapCenter?> GetMapCenterAsync(string elementId);
    Task DestroyMapAsync(string elementId);
    Task<bool> GeocodeAddressAsync(string address);
    Task<bool> ReverseGeocodeAsync(decimal latitude, decimal longitude);
}

public class GoogleMapsService : IGoogleMapsService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<GoogleMapsService> _logger;
    private IJSObjectReference? _googleMapsModule;
    private DotNetObjectReference<GoogleMapsService>? _dotNetReference;
    
    // Events for JavaScript callbacks
    public event Func<decimal, decimal, Task>? MapClicked;
    public event Func<decimal, decimal, Task>? MarkerDragged;
    public event Func<GoogleGeocodeResult, Task>? GeocodeCompleted;
    public event Func<string, Task>? GeocodeError;
    public event Func<string, Task>? ReverseGeocodeCompleted;
    public event Func<string, Task>? ReverseGeocodeError;

    public GoogleMapsService(IJSRuntime jsRuntime, ILogger<GoogleMapsService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _dotNetReference = DotNetObjectReference.Create(this);
    }

    public async Task<bool> IsApiLoadedAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.isApiLoaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Google Maps API is loaded");
            return false;
        }
    }

    public async Task<bool> InitializeMapAsync(string elementId, decimal latitude, decimal longitude, int zoom = 10)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.initializeMap", 
                elementId, (double)latitude, (double)longitude, zoom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing map for element {ElementId}", elementId);
            return false;
        }
    }

    public async Task<GoogleMapMarker?> AddMarkerAsync(string elementId, decimal latitude, decimal longitude, string? title = null, bool draggable = false)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<GoogleMapMarker?>("googleMapsInterop.addMarker", 
                elementId, (double)latitude, (double)longitude, title, draggable);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding marker to map {ElementId}", elementId);
            return null;
        }
    }

    public async Task ClearMarkersAsync(string elementId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("googleMapsInterop.clearMarkers", elementId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing markers from map {ElementId}", elementId);
        }
    }

    public async Task<bool> UpdateMarkerAsync(string elementId, int markerId, decimal latitude, decimal longitude)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.updateMarker", 
                elementId, markerId, (double)latitude, (double)longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating marker {MarkerId} on map {ElementId}", markerId, elementId);
            return false;
        }
    }

    public async Task<bool> SetupClickListenerAsync(string elementId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.setupClickListener", 
                elementId, _dotNetReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up click listener for map {ElementId}", elementId);
            return false;
        }
    }

    public async Task<bool> SetupMarkerDragListenerAsync(string elementId, int markerId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.setupMarkerDragListener", 
                elementId, markerId, _dotNetReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up marker drag listener for map {ElementId}", elementId);
            return false;
        }
    }

    public async Task<bool> CenterMapAsync(string elementId, decimal latitude, decimal longitude, int? zoom = null)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.centerMap", 
                elementId, (double)latitude, (double)longitude, zoom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error centering map {ElementId}", elementId);
            return false;
        }
    }

    public async Task<GoogleMapCenter?> GetMapCenterAsync(string elementId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<GoogleMapCenter?>("googleMapsInterop.getMapCenter", elementId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting map center for {ElementId}", elementId);
            return null;
        }
    }

    public async Task DestroyMapAsync(string elementId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("googleMapsInterop.destroyMap", elementId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error destroying map {ElementId}", elementId);
        }
    }

    public async Task<bool> GeocodeAddressAsync(string address)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.geocodeAddress", 
                address, _dotNetReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return false;
        }
    }

    public async Task<bool> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("googleMapsInterop.reverseGeocode", 
                (double)latitude, (double)longitude, _dotNetReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Lat}, {Lng}", latitude, longitude);
            return false;
        }
    }

    // JavaScript callback methods
    [JSInvokable]
    public async Task OnMapClicked(double latitude, double longitude)
    {
        if (MapClicked != null)
        {
            await MapClicked.Invoke((decimal)latitude, (decimal)longitude);
        }
    }

    [JSInvokable]
    public async Task OnMarkerDragged(double latitude, double longitude)
    {
        if (MarkerDragged != null)
        {
            await MarkerDragged.Invoke((decimal)latitude, (decimal)longitude);
        }
    }

    [JSInvokable]
    public async Task OnGeocodeSuccess(GoogleGeocodeResult result)
    {
        if (GeocodeCompleted != null)
        {
            await GeocodeCompleted.Invoke(result);
        }
    }

    [JSInvokable]
    public async Task OnGeocodeError(string error)
    {
        if (GeocodeError != null)
        {
            await GeocodeError.Invoke(error);
        }
    }

    [JSInvokable]
    public async Task OnReverseGeocodeSuccess(string address)
    {
        if (ReverseGeocodeCompleted != null)
        {
            await ReverseGeocodeCompleted.Invoke(address);
        }
    }

    [JSInvokable]
    public async Task OnReverseGeocodeError(string error)
    {
        if (ReverseGeocodeError != null)
        {
            await ReverseGeocodeError.Invoke(error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_googleMapsModule != null)
        {
            await _googleMapsModule.DisposeAsync();
        }
        
        _dotNetReference?.Dispose();
    }
}

// Supporting data models for Google Maps
public class GoogleMapMarker
{
    public int MarkerId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}

public class GoogleMapCenter
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public int Zoom { get; set; }
}

public class GoogleGeocodeResult
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
}