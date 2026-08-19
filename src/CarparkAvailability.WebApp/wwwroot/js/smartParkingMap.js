window.smartParkingMap = (() => {
    let map;
    let infoWindow;
    let dotNetRef;
    let markers = [];
    let markerLookup = new Map();

    async function waitForGoogleMaps() {
        const timeoutAt = Date.now() + 15000;
        while (!(window.google && window.google.maps && window.google.maps.places)) {
            if (Date.now() > timeoutAt) {
                throw new Error("Google Maps failed to load.");
            }

            await new Promise(resolve => setTimeout(resolve, 100));
        }
    }

    function clearMarkers() {
        for (const marker of markers) {
            marker.setMap(null);
        }

        markers = [];
        markerLookup.clear();
    }

    function createMarkerIcon(carPark) {
        const fill = carPark.isStale ? "#f59e0b" : carPark.availableLots > 0 ? "#0f766e" : "#64748b";
        return {
            path: google.maps.SymbolPath.CIRCLE,
            fillColor: fill,
            fillOpacity: 1,
            scale: 8,
            strokeColor: "#ffffff",
            strokeWeight: 2
        };
    }

    return {
        initialize: async (mapElement, inputElement, reference) => {
            await waitForGoogleMaps();
            dotNetRef = reference;
            map = new google.maps.Map(mapElement, {
                center: { lat: 1.3521, lng: 103.8198 },
                zoom: 12,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false
            });
            infoWindow = new google.maps.InfoWindow();

            const autocomplete = new google.maps.places.Autocomplete(inputElement, {
                bounds: new google.maps.LatLngBounds(
                    { lat: 1.1304753, lng: 103.6053215 },
                    { lat: 1.470759, lng: 104.0884808 }),
                componentRestrictions: { country: "sg" },
                fields: ["formatted_address", "geometry", "name"],
                strictBounds: false
            });

            autocomplete.addListener("place_changed", async () => {
                const place = autocomplete.getPlace();
                if (!place || !place.geometry || !place.geometry.location) {
                    return;
                }

                const label = place.formatted_address || place.name || "Selected destination";
                await dotNetRef.invokeMethodAsync(
                    "OnPlaceSelected",
                    label,
                    place.geometry.location.lat(),
                    place.geometry.location.lng());
            });
        },

        updateMarkers: (carParks, selectedCarParkNo) => {
            if (!map) {
                return;
            }

            clearMarkers();

            if (!carParks || carParks.length === 0) {
                return;
            }

            const bounds = new google.maps.LatLngBounds();

            for (const carPark of carParks) {
                const marker = new google.maps.Marker({
                    map,
                    position: { lat: carPark.latitude, lng: carPark.longitude },
                    title: `${carPark.carParkNo} · ${carPark.address}`,
                    icon: createMarkerIcon(carPark)
                });

                marker.addListener("click", async () => {
                    await dotNetRef.invokeMethodAsync("OnMarkerSelected", carPark.carParkNo);
                });

                markers.push(marker);
                markerLookup.set(carPark.carParkNo, marker);
                bounds.extend(marker.getPosition());

                if (carPark.carParkNo === selectedCarParkNo) {
                    infoWindow.setContent(`<strong>${carPark.carParkNo}</strong><br/>${carPark.address}`);
                    infoWindow.open({ map, anchor: marker });
                }
            }

            if (carParks.length === 1) {
                map.setCenter(bounds.getCenter());
                map.setZoom(15);
            } else {
                map.fitBounds(bounds, 72);
            }
        },

        centerOnPlace: (latitude, longitude) => {
            if (!map) {
                return;
            }

            map.panTo({ lat: latitude, lng: longitude });
            if (map.getZoom() < 14) {
                map.setZoom(14);
            }
        },

        focusMarker: (carParkNo, latitude, longitude) => {
            if (!map) {
                return;
            }

            const marker = markerLookup.get(carParkNo);
            map.panTo({ lat: latitude, lng: longitude });
            if (map.getZoom() < 15) {
                map.setZoom(15);
            }

            if (marker) {
                infoWindow.setContent(marker.getTitle());
                infoWindow.open({ map, anchor: marker });
            }
        },

        dispose: () => {
            clearMarkers();
            infoWindow = undefined;
            dotNetRef = undefined;
            map = undefined;
        }
    };
})();
