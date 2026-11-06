
document.addEventListener("DOMContentLoaded", function () {
    const mapElement = document.getElementById("incidentMap");

    if (!mapElement) return;

    const latitudeInput = document.getElementById("Latitude");
    const longitudeInput = document.getElementById("Longitude");
    const detectButton = document.getElementById("detectLocation");
    const message = document.getElementById("locationMessage");

    if (!latitudeInput || !longitudeInput || !message) {
        console.error("SafeRoute SA: Missing GPS form elements.");
        return;
    }

    if (typeof L === "undefined") {
        message.textContent =
            "Map could not load. Check your internet connection.";
        return;
    }

    if (mapElement.dataset.mapInitialized === "true") return;
    mapElement.dataset.mapInitialized = "true";

    // South Africa default map view
    const map = L.map(mapElement, {
        center: [-29.0, 24.0],
        zoom: 5,
        zoomControl: false,
        scrollWheelZoom: false,
        tap: true
    });

    // Standard OpenStreetMap layer
    L.tileLayer(
        "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
        {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19,
            crossOrigin: true
        }
    ).addTo(map);

    // Custom zoom controls
    L.control.zoom({
        position: "bottomright"
    }).addTo(map);

    // Professional blue location pin
    const locationIcon = L.divIcon({
        className: "sr-location-marker",
        html: `
            <div class="sr-marker-pulse"></div>
            <div class="sr-marker-pin">
                <svg viewBox="0 0 24 24"
                     width="22"
                     height="22"
                     fill="none"
                     stroke="white"
                     stroke-width="2.5"
                     stroke-linecap="round"
                     stroke-linejoin="round">
                    <path d="M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Z"/>
                    <circle cx="12" cy="10" r="2.5"/>
                </svg>
            </div>
        `,
        iconSize: [48, 48],
        iconAnchor: [24, 42]
    });

    let marker = null;
    let detectedPosition = null;

    // Optional coordinates display
    const coordinateDisplay =
        document.getElementById("selectedCoordinates");

    // Optional confirmation badge
    const locationStatus =
        document.getElementById("locationStatus");

    function setMessage(text, type = "info") {
        message.textContent = text;
        message.dataset.status = type;

        if (locationStatus) {
            locationStatus.textContent =
                type === "success"
                    ? "Location selected"
                    : type === "error"
                        ? "Location unavailable"
                        : "Choose location";

            locationStatus.dataset.status = type;
        }
    }

    function refreshMap() {
        map.invalidateSize({
            pan: false,
            animate: false
        });
    }

    function updateCoordinates(latitude, longitude) {
        const lat = Number(latitude);
        const lng = Number(longitude);

        if (
            !Number.isFinite(lat) ||
            !Number.isFinite(lng) ||
            lat < -90 || lat > 90 ||
            lng < -180 || lng > 180
        ) {
            setMessage("Invalid location coordinates.", "error");
            return false;
        }

        latitudeInput.value = lat.toFixed(6);
        longitudeInput.value = lng.toFixed(6);

        if (coordinateDisplay) {
            coordinateDisplay.textContent =
                `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
        }

        if (marker) {
            marker.setLatLng([lat, lng]);
        } else {
            marker = L.marker([lat, lng], {
                icon: locationIcon,
                draggable: true,
                autoPan: true
            }).addTo(map);

            marker.on("dragend", function () {
                const position = marker.getLatLng();

                if (updateCoordinates(
                    position.lat,
                    position.lng
                )) {
                    setMessage(
                        "Incident location updated successfully.",
                        "success"
                    );
                }
            });
        }

        return true;
    }

    // Click anywhere on the map to choose a location
    map.on("click", function (event) {
        if (updateCoordinates(
            event.latlng.lat,
            event.latlng.lng
        )) {
            setMessage(
                "Location selected. Drag the pin to adjust it.",
                "success"
            );
        }
    });

    // Detect GPS only after the user clicks the button
    if (detectButton) {
        detectButton.addEventListener("click", function () {
            if (!navigator.geolocation) {
                setMessage(
                    "GPS is unavailable. Select a location on the map.",
                    "error"
                );
                return;
            }

            detectButton.disabled = true;
            detectButton.classList.add("sr-gps-loading");

            setMessage("Detecting your current location...");

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    const lat = position.coords.latitude;
                    const lng = position.coords.longitude;

                    detectedPosition = [lat, lng];

                    if (updateCoordinates(lat, lng)) {
                        refreshMap();

                        map.setView([lat, lng], 16, {
                            animate: true
                        });

                        setMessage(
                            "Location detected. Drag the pin if the incident happened elsewhere.",
                            "success"
                        );
                    }

                    detectButton.disabled = false;
                    detectButton.classList.remove(
                        "sr-gps-loading"
                    );
                },
                function (error) {
                    let errorMessage =
                        "Unable to detect your location.";

                    if (error.code === 1) {
                        errorMessage =
                            "Location permission denied.";
                    } else if (error.code === 2) {
                        errorMessage =
                            "Location information unavailable.";
                    } else if (error.code === 3) {
                        errorMessage =
                            "Location request timed out.";
                    }

                    setMessage(
                        errorMessage +
                        " Select the incident location on the map.",
                        "error"
                    );

                    detectButton.disabled = false;
                    detectButton.classList.remove(
                        "sr-gps-loading"
                    );
                },
                {
                    enableHighAccuracy: true,
                    timeout: 15000,
                    maximumAge: 0
                }
            );
        });
    }

    // Optional recenter button
    const recenterButton =
        document.getElementById("recenterLocation");

    if (recenterButton) {
        recenterButton.addEventListener("click", function () {
            if (marker) {
                refreshMap();
                map.setView(marker.getLatLng(), 16);
            } else if (detectedPosition) {
                refreshMap();
                map.setView(detectedPosition, 16);
            } else {
                setMessage(
                    "Select a location or use Detect My Location first."
                );
            }
        });
    }

    // Restore coordinates after validation errors
    if (latitudeInput.value && longitudeInput.value) {
        const lat = Number(latitudeInput.value);
        const lng = Number(longitudeInput.value);

        if (updateCoordinates(lat, lng)) {
            map.setView([lat, lng], 16);

            setMessage(
                "Your previously selected location has been restored.",
                "success"
            );
        }
    }

    // Fix incomplete tiles after rendering or resizing
    requestAnimationFrame(refreshMap);

    window.addEventListener("load", function () {
        setTimeout(refreshMap, 250);
        setTimeout(refreshMap, 900);
    });

    window.addEventListener("resize", refreshMap);

    // ResizeObserver detects changes in the map container
    if (typeof ResizeObserver !== "undefined") {
        const observer = new ResizeObserver(function () {
            refreshMap();
        });

        observer.observe(mapElement);
    }
});
