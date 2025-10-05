// Google Maps JavaScript Interop for Blazor
// Provides JavaScript functions that can be called from C# to interact with Google Maps

window.googleMapsInterop = {
    maps: new Map(), // Store map instances by elementId
    markers: new Map(), // Store marker instances by mapId
    
    // Initialize Google Maps API callback
    initMap: function() {
        console.log('Google Maps API loaded successfully');
        window.googleMapsApiLoaded = true;
    },

    // Check if Google Maps API is loaded
    isApiLoaded: function() {
        return typeof google !== 'undefined' && typeof google.maps !== 'undefined';
    },

    // Initialize a map in the specified container
    initializeMap: function(elementId, latitude, longitude, zoom) {
        if (!this.isApiLoaded()) {
            console.error('Google Maps API not loaded');
            return false;
        }

        const mapElement = document.getElementById(elementId);
        if (!mapElement) {
            console.error(`Element with id '${elementId}' not found`);
            return false;
        }

        const mapOptions = {
            center: { lat: latitude, lng: longitude },
            zoom: zoom || 10,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            streetViewControl: false,
            mapTypeControl: true,
            fullscreenControl: true,
            zoomControl: true
        };

        const map = new google.maps.Map(mapElement, mapOptions);
        this.maps.set(elementId, map);
        
        console.log(`Map initialized for element: ${elementId}`);
        return true;
    },

    // Add a marker to the map
    addMarker: function(elementId, latitude, longitude, title, draggable) {
        const map = this.maps.get(elementId);
        if (!map) {
            console.error(`Map not found for element: ${elementId}`);
            return null;
        }

        const marker = new google.maps.Marker({
            position: { lat: latitude, lng: longitude },
            map: map,
            title: title || 'Location',
            draggable: draggable || false
        });

        // Store marker reference
        let mapMarkers = this.markers.get(elementId) || [];
        mapMarkers.push(marker);
        this.markers.set(elementId, mapMarkers);

        return {
            markerId: mapMarkers.length - 1,
            lat: latitude,
            lng: longitude
        };
    },

    // Clear all markers from a map
    clearMarkers: function(elementId) {
        const markers = this.markers.get(elementId) || [];
        markers.forEach(marker => marker.setMap(null));
        this.markers.set(elementId, []);
    },

    // Update marker position
    updateMarker: function(elementId, markerId, latitude, longitude) {
        const markers = this.markers.get(elementId) || [];
        if (markerId >= 0 && markerId < markers.length) {
            const marker = markers[markerId];
            marker.setPosition({ lat: latitude, lng: longitude });
            return true;
        }
        return false;
    },

    // Set up click listener for coordinate selection
    setupClickListener: function(elementId, dotNetObjectRef) {
        const map = this.maps.get(elementId);
        if (!map) {
            console.error(`Map not found for element: ${elementId}`);
            return false;
        }

        map.addListener('click', function(event) {
            const lat = event.latLng.lat();
            const lng = event.latLng.lng();
            
            // Call back to C# method
            dotNetObjectRef.invokeMethodAsync('OnMapClicked', lat, lng);
        });

        return true;
    },

    // Set up marker drag listener
    setupMarkerDragListener: function(elementId, markerId, dotNetObjectRef) {
        const markers = this.markers.get(elementId) || [];
        if (markerId >= 0 && markerId < markers.length) {
            const marker = markers[markerId];
            
            marker.addListener('dragend', function(event) {
                const lat = event.latLng.lat();
                const lng = event.latLng.lng();
                
                // Call back to C# method
                dotNetObjectRef.invokeMethodAsync('OnMarkerDragged', lat, lng);
            });
            
            return true;
        }
        return false;
    },

    // Center map on coordinates
    centerMap: function(elementId, latitude, longitude, zoom) {
        const map = this.maps.get(elementId);
        if (!map) {
            console.error(`Map not found for element: ${elementId}`);
            return false;
        }

        map.setCenter({ lat: latitude, lng: longitude });
        if (zoom) {
            map.setZoom(zoom);
        }
        return true;
    },

    // Get current map center
    getMapCenter: function(elementId) {
        const map = this.maps.get(elementId);
        if (!map) {
            return null;
        }

        const center = map.getCenter();
        return {
            lat: center.lat(),
            lng: center.lng(),
            zoom: map.getZoom()
        };
    },

    // Cleanup map instance
    destroyMap: function(elementId) {
        // Clear markers first
        this.clearMarkers(elementId);
        
        // Remove map instance
        this.maps.delete(elementId);
        this.markers.delete(elementId);
        
        console.log(`Map destroyed for element: ${elementId}`);
    },

    // Geocoding - Get coordinates from address
    geocodeAddress: function(address, dotNetObjectRef) {
        if (!this.isApiLoaded()) {
            console.error('Google Maps API not loaded');
            return false;
        }

        const geocoder = new google.maps.Geocoder();
        geocoder.geocode({ address: address }, function(results, status) {
            if (status === 'OK' && results.length > 0) {
                const location = results[0].geometry.location;
                const formattedAddress = results[0].formatted_address;
                
                dotNetObjectRef.invokeMethodAsync('OnGeocodeSuccess', {
                    lat: location.lat(),
                    lng: location.lng(),
                    formattedAddress: formattedAddress
                });
            } else {
                dotNetObjectRef.invokeMethodAsync('OnGeocodeError', `Geocoding failed: ${status}`);
            }
        });

        return true;
    },

    // Reverse geocoding - Get address from coordinates
    reverseGeocode: function(latitude, longitude, dotNetObjectRef) {
        if (!this.isApiLoaded()) {
            console.error('Google Maps API not loaded');
            return false;
        }

        const geocoder = new google.maps.Geocoder();
        const latlng = { lat: latitude, lng: longitude };
        
        geocoder.geocode({ location: latlng }, function(results, status) {
            if (status === 'OK' && results.length > 0) {
                const formattedAddress = results[0].formatted_address;
                
                dotNetObjectRef.invokeMethodAsync('OnReverseGeocodeSuccess', formattedAddress);
            } else {
                dotNetObjectRef.invokeMethodAsync('OnReverseGeocodeError', `Reverse geocoding failed: ${status}`);
            }
        });

        return true;
    }
};

// Global callback function for Google Maps API
function initMap() {
    window.googleMapsInterop.initMap();
}

// Make sure API is available globally
window.initMap = initMap;