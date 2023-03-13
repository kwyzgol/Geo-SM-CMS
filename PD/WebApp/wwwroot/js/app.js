var scrollValue = window.scrollY;

var mapMarker = null;
var map = null;

window.onload = () => {
    var theme = window.localStorage.getItem("theme");
    var html = document.getElementsByTagName("html")[0];

    if (theme) html.setAttribute("data-theme", theme);
    else {
        if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) {
            localStorage.setItem("theme", "dark");
            html.setAttribute("data-theme", "dark");
        } else {
            localStorage.setItem("theme", "light");
            html.setAttribute("data-theme", "light");
        }
    }
};

function changeTheme() {
    var theme = window.localStorage.getItem("theme");
    var html = document.getElementsByTagName("html")[0];
    if (theme === "light") {
        localStorage.setItem("theme", "dark");
        html.setAttribute("data-theme", "dark");
    } else {
        localStorage.setItem("theme", "light");
        html.setAttribute("data-theme", "light");
    }
}

function openMobileMenu() {
    var menuButton = document.getElementById("mobile-button-menu");
    var closeButton = document.getElementById("mobile-button-close");
    var pageLeft = document.getElementById("page-left");
    var pageRight = document.getElementById("page-right");

    scrollValue = window.scrollY;

    menuButton.classList.add("mobile-hidden");
    closeButton.classList.remove("mobile-hidden");

    pageLeft.classList.add("mobile-menu-opened");
    pageLeft.classList.remove("mobile-menu-closed");

    pageRight.classList.add("mobile-hidden");

    window.scrollTo(0, 0);
}

function closeMobileMenu() {
    var menuButton = document.getElementById("mobile-button-menu");
    var closeButton = document.getElementById("mobile-button-close");
    var pageLeft = document.getElementById("page-left");
    var pageRight = document.getElementById("page-right");

    closeButton.classList.add("mobile-hidden");
    menuButton.classList.remove("mobile-hidden");

    pageLeft.classList.add("mobile-menu-closed");
    pageLeft.classList.remove("mobile-menu-opened");

    window.scrollTo(0, scrollValue);

    pageRight.classList.remove("mobile-hidden");
}

function CloseMobileMenuAfterRedirect() {
    scrollValue = window.scrollY;
    closeMobileMenu();
}

function ShowMap(tilesUrl, attribution, id, latitude, longitude, zoom) {
    try {
        map = null;
        mapMarker = null;
        map = L.map(id).setView([latitude, longitude], zoom);
        var tiles = L.tileLayer(tilesUrl,
            {
                maxZoom: 19,
                attribution: attribution
            }).addTo(map);
        map.on("click", OnMapClick);
    } catch (e) {
        console.log(e.message);
    }
}

function OnMapClick(e) {
    if (mapMarker != null) mapMarker.remove();

    var latitudeInput = document.getElementById("latitudeModalMap");
    var longitudeInput = document.getElementById("longitudeModalMap");

    latitudeInput.value = e.latlng.lat;
    longitudeInput.value = e.latlng.lng;

    latitudeInput.dispatchEvent(new Event("change"));
    longitudeInput.dispatchEvent(new Event("change"));

    mapMarker = L.marker(e.latlng);
    mapMarker.addTo(map);
}

function SetPlace(latitude, longitude) {
    var latlng = L.latLng(latitude, longitude);
    if (mapMarker != null) mapMarker.remove();
    map.flyTo(latlng); //, 15);
    mapMarker = L.marker(latlng);
    map.addLayer(mapMarker);

    var latitudeInput = document.getElementById("latitudeModalMap");
    var longitudeInput = document.getElementById("longitudeModalMap");

    latitudeInput.value = latitude;
    longitudeInput.value = longitude;

    latitudeInput.dispatchEvent(new Event("change"));
    longitudeInput.dispatchEvent(new Event("change"));
}

function GetGeolocation() {
    navigator.geolocation.getCurrentPosition(GeolocationSuccess, GeolocationError);
}

function GeolocationSuccess(geolocation) {
    var latitude = geolocation.coords.latitude;
    var longitude = geolocation.coords.longitude;

    var latitudeInput = document.getElementById("latitude");
    var longitudeInput = document.getElementById("longitude");

    latitudeInput.value = latitude;
    longitudeInput.value = longitude;

    latitudeInput.dispatchEvent(new Event("change"));
    longitudeInput.dispatchEvent(new Event("change"));
}

function GeolocationError(error) {
    console.log(error);
}

function ScrollToBottom() {
    window.scrollTo(0, document.body.scrollHeight);
}

function TextAreaEnter(event) {
    if (event.key === "Enter") {
        if (event.shiftKey) {
            return;
        } else {
            var button = document.getElementById("send-message");
            var textarea = document.getElementById("message-textarea");
            textarea.dispatchEvent(new Event("change"));
            button.click();
            event.preventDefault();
        }
    }
}