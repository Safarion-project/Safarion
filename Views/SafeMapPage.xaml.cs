using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Safarion.Models;
using Safarion.Services;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace Safarion.Views
{
    public partial class SafeMapPage : ContentPage
    {
        private Location? _myLocation;
        private Location? _dangerZone = null;
        private bool _isInDanger = false;
        private bool _isTrapSet = false;

        // NEW: Forces the camera to Zoom immediately on startup
        private bool _isFirstZoom = true;
        // NEW: Timer for Real-Time Updates
        private IDispatcherTimer _timer;

        public SafeMapPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 1. SMART NAME CHECK
            // If name is missing OR if it is the old "default" test name, ask again!
            string currentName = Preferences.Get("UserName", string.Empty);

            if (string.IsNullOrEmpty(currentName) || currentName == "User (Explorer)" || currentName == "Rahul (Explorer)")
            {
                string result = await DisplayPromptAsync("Setup Profile", "What is your Name?", "Save", "Later");

                // If they typed something valid, save it.
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Preferences.Set("UserName", result);
                }
                else if (string.IsNullOrEmpty(currentName))
                {
                    // Fallback if they hit Cancel but have NO name
                    Preferences.Set("UserName", "Traveler");
                }
            }

            await StartTracking();
            await LoadSafetyReviews(); // NEW: Load review markers

            // 2. START TIMER
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await RefreshTravelers();
            _timer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Geolocation.LocationChanged -= OnLocationChanged;
            try { Geolocation.StopListeningForeground(); } catch { }
            _timer?.Stop();
        }

        private async Task StartTracking()
        {
            try
            {
                // ✅ ADD THIS FIRST
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    CoordsLabel.Text = "Location permission denied.";
                    return;
                }

                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location != null) UpdateMap(location);

                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(2));
                Geolocation.LocationChanged += OnLocationChanged;
                await Geolocation.StartListeningForegroundAsync(request);
            }
            catch (Exception ex)
            {
                CoordsLabel.Text = "GPS WAITING...";
                System.Diagnostics.Debug.WriteLine($"[SafeMap] StartTracking error: {ex.Message}");
            }
        }

        private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            UpdateMap(e.Location);

            // 1. Set Danger Zone (Only once)
            if (!_isTrapSet)
            {
                _dangerZone = new Location(e.Location.Latitude + 0.0005, e.Location.Longitude);
                DrawDangerZone();
                _isTrapSet = true;
            }

            // 2. Check Danger
            CheckDangerZone(e.Location);

            // FIX 1: Upload REAL Name (e.g., "Samsung S21") instead of "Friend"
            // This prevents the "Invisible Friend" bug
            // FIX: Removed 'DeviceInfo.Name'. Only sending Location and Status now.
            try { RealtimeTravelerService.UploadMyLocation(e.Location, "Active 🟢"); } catch { }
        }

        private void UpdateMap(Location location)
        {
            _myLocation = location;
            CoordsLabel.Text = $"{location.Latitude:F5}, {location.Longitude:F5}";

            // 1. Ensure "Me" Pin exists
            var mePin = SafetyMap.Pins.FirstOrDefault(p => p.Label == "Me");
            if (mePin == null)
            {
                mePin = new Pin { Label = "Me", Type = PinType.SavedPin, Location = location };
                SafetyMap.Pins.Add(mePin);
            }
            else
            {
                mePin.Location = location; // Just move it, don't recreate it
            }

            // --- NEW AUTO-ZOOM LOGIC ---
            // If this is the first time the app is loading, SNAP the camera close!
            if (_isFirstZoom)
            {
                // Zoom level: 0.2 km (200 meters) - Very close
                SafetyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.15)));
                _isFirstZoom = false; // Stop zooming automatically so user can pan around
            }
        }

        // --- REAL-TIME FRIEND REFRESHER ---
        private async Task RefreshTravelers()
        {
            if (_myLocation == null) return;

            try
            {
                string myId = RealtimeTravelerService.GetMyUserId();
                var travelers = await RealtimeTravelerService.GetAllTravelers();

                // Loop through downloaded friends
                foreach (var item in travelers)
                {
                    if (item.Key == myId) continue; // Skip myself

                    // Check if pin already exists
                    var friendPin = SafetyMap.Pins.FirstOrDefault(p =>
   p.Label == item.Value.Name && p.Type == PinType.SavedPin);

                    if (friendPin == null)
                    {
                        // CREATE NEW PIN
                        friendPin = new Pin
                        {
                            Label = item.Value.Name, // 👈 NO emoji (clean look)
                            Address = "Tap for Options",
                            Type = PinType.SavedPin, // 👈 gives proper map pin feel
                            Location = item.Value.Position
                        };
                        friendPin.MarkerClicked += OnTravelerPinClicked;
                        SafetyMap.Pins.Add(friendPin);
                    }
                    else
                    {
                        // UPDATE EXISTING PIN (Smooth movement)
                        friendPin.Location = item.Value.Position;
                    }
                }
            }
            catch { }
        }

        // --- GEOFENCING & SAFETY LOGIC ---
        private void DrawDangerZone()
        {
            if (_dangerZone == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var oldCircles = SafetyMap.MapElements.Where(x => x is Circle).ToList();
                foreach (var old in oldCircles) SafetyMap.MapElements.Remove(old);

                var circle = new Circle
                {
                    Center = _dangerZone,
                    Radius = Distance.FromMeters(100),
                    StrokeColor = Colors.Red,
                    StrokeWidth = 8,
                    FillColor = Color.FromRgba("#88FF0000")
                };
                SafetyMap.MapElements.Add(circle);
            });
        }

        private void CheckDangerZone(Location currentLocation)
        {
            if (_dangerZone == null) return;
            double distance = Location.CalculateDistance(currentLocation, _dangerZone, DistanceUnits.Kilometers);

            if (distance < 0.1)
            {
                if (!_isInDanger)
                {
                    _isInDanger = true;
                    TriggerAlert();
                }
            }
            else { _isInDanger = false; }
        }

        private void TriggerAlert()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { if (Vibration.Default.IsSupported) Vibration.Default.Vibrate(TimeSpan.FromSeconds(2)); } catch { }
                await DisplayAlert("⚠️ WARNING", "HIGH RISK ZONE!", "OK");
            });
        }

        // --- APP LAUNCHER LOGIC ---

        private async void OnShowPoliceClicked(object sender, TappedEventArgs e)
        {
            await Launcher.OpenAsync("geo:0,0?q=Police+Station");
        }

        private async void OnShowHospitalsClicked(object sender, TappedEventArgs e)
        {
            await Launcher.OpenAsync("geo:0,0?q=Hospital");
        }

        private async void OnShowRestaurantsClicked(object sender, TappedEventArgs e)
        {
            try
            {
                bool opened = await Launcher.OpenAsync("zomato://");
                if (!opened) opened = await Launcher.OpenAsync("swiggy://");
                if (!opened) throw new Exception();
            }
            catch
            {
                await Launcher.OpenAsync("geo:0,0?q=Restaurants");
            }
        }

        private async void OnFindTravelersClicked(object sender, TappedEventArgs e)
        {
            if (_myLocation == null)
            {
                await DisplayAlert("Error", "GPS not found", "OK");
                return;
            }

            try
            {
                // 1. Get MY Unique ID
                string myId = RealtimeTravelerService.GetMyUserId();

                // 2. Upload (Name is auto-detected)
                await RealtimeTravelerService.UploadMyLocation(_myLocation, "Exploring Live 🔴");

                // 3. Download Dictionary
                var travelersDict = await RealtimeTravelerService.GetAllTravelers();

                var pinsToRemove = SafetyMap.Pins
    .Where(p => p.Type == PinType.Generic)
    .ToList();

                foreach (var p in pinsToRemove)
                    SafetyMap.Pins.Remove(p);

                int count = 0;

                // 4. Loop through Dictionary
                foreach (var item in travelersDict)
                {
                    // Skip myself
                    if (item.Key == myId) continue;

                    var friendPin = new Pin
                    {
                        Label = item.Value.Name,
                        Address = "Click for Options",
                        Type = PinType.SavedPin, // 👈 IMPORTANT CHANGE
                        Location = item.Value.Position
                    };

                    // Attach the Click Event
                    friendPin.MarkerClicked += OnTravelerPinClicked;

                    SafetyMap.Pins.Add(friendPin);
                    count++;
                }
                await DisplayAlert("Travelers Found", $"{count} active users.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Connection Failed", "Could not reach the cloud. Checking simulation...", "OK");
            }
        }

        // --- NEW: PIN CLICK MENU (Navigate, Chat, Add Friend) ---
        private async void OnTravelerPinClicked(object sender, PinClickedEventArgs e)
        {
            e.HideInfoWindow = true; // Stop the standard label from popping up
            var pin = (Pin)sender;

            // We need the UserID to chat. Since we didn't store it in the Pin directly, 
            // we will quickly find it again from the list.
            // Find the User ID for this pin name
            var travelers = await RealtimeTravelerService.GetAllTravelers();
            string targetUserId = travelers.FirstOrDefault(x => x.Value.Name == pin.Label).Key;

            string action = await DisplayActionSheet($"Options for {pin.Label}", "Cancel", null,
                "Navigate 📍", "Chat 💬", "Add Friend ➕");

            if (action == "Navigate 📍")
            {
                await Launcher.OpenAsync($"google.navigation:q={pin.Location.Latitude},{pin.Location.Longitude}&mode=w");
            }
            else if (action == "Chat 💬")
            {
                if (!string.IsNullOrEmpty(targetUserId))
                    await Navigation.PushAsync(new TravelerChatPage(targetUserId, pin.Label));
            }
            else if (action == "Add Friend ➕")
            {
                if (!string.IsNullOrEmpty(targetUserId))
                {
                    bool alreadyFriend = await RealtimeTravelerService
    .IsAlreadyFriend(targetUserId);

                    if (alreadyFriend)
                    {
                        bool goChat = await DisplayAlert(
                            "Already Friends! 🎉",
                            $"You and {pin.Label} are already connected.\nWant to open a chat?",
                            "Open Chat", "Close");

                        if (goChat)
                            await Navigation.PushAsync(
                                new TravelerChatPage(targetUserId, pin.Label));
                    }
                    else
                    {
                        await RealtimeTravelerService.SendFriendRequest(targetUserId);
                        await DisplayAlert("Sent!", "Friend request sent!", "OK");
                    }
                }
            }
        }

        private void OnNavigateHomeClicked(object sender, TappedEventArgs e)
        {
            if (_myLocation == null) return;
            SafetyMap.MapElements.Clear();
            DrawDangerZone();

            var dest = new Location(_myLocation.Latitude + 0.002, _myLocation.Longitude);
            var path = new Polyline
            {
                StrokeColor = Colors.Green,
                StrokeWidth = 10,
                Geopath = { _myLocation, new Location(_myLocation.Latitude, _myLocation.Longitude + 0.001), dest }
            };
            SafetyMap.MapElements.Add(path);
            SafetyMap.Pins.Add(new Pin { Label = "Safe Home", Type = PinType.SavedPin, Location = dest });
        }

        private async void OnSimulateDangerClicked(object sender, EventArgs e)
        {
            // 1. If we don't even have the "Green Numbers" yet, we truly must wait.
            if (_myLocation == null)
            {
                await DisplayAlert("Wait", "Waiting for GPS signal...", "OK");
                return;
            }

            // 2. FIX: If we have a location but the Trap isn't set yet (because we haven't moved),
            //    create it RIGHT NOW based on where we are standing.
            if (_dangerZone == null)
            {
                _dangerZone = new Location(_myLocation.Latitude + 0.0005, _myLocation.Longitude);
                DrawDangerZone();
                _isTrapSet = true;
            }

            // 3. Now force the simulation
            // We set our location to the danger zone's coordinates to trigger the alert
            var fakeLocation = new Location(_dangerZone.Latitude, _dangerZone.Longitude);

            // Update UI to show we are "Teleporting"
            CoordsLabel.Text = $"SIMULATED: {fakeLocation.Latitude:F5}, {fakeLocation.Longitude:F5}";

            CheckDangerZone(fakeLocation);
        }

        private async void OnShowReviewsClicked(object sender, TappedEventArgs e)
        {
            await LoadSafetyReviews();
        }


        // SAFETY REVIEWS ON MAP
        private async Task LoadSafetyReviews()
        {
            try
            {
                // ✅ Remove ONLY review pins (safe removal)
                var oldReviewPins = SafetyMap.Pins
                    .Where(p => p.Label.StartsWith("💬"))
                    .ToList();

                foreach (var pin in oldReviewPins)
                {
                    SafetyMap.Pins.Remove(pin);
                }

                var reviews = await ReviewService.GetReviews();

                var reviewsWithLocation = reviews
                    .Where(r => r.Latitude != 0 && r.Longitude != 0)
                    .ToList();

                foreach (var review in reviewsWithLocation)
                {
                    string icon = "💬";
                    string comment = review.Comment?.ToLower() ?? "";

                    // 🔥 CATEGORY CLASSIFICATION
                    if (comment.Contains("food") || comment.Contains("restaurant") || comment.Contains("hotel"))
                        icon = "🍜";
                    else if (comment.Contains("beautiful") || comment.Contains("view") || comment.Contains("spot"))
                        icon = "📸";
                    else if (comment.Contains("danger") || comment.Contains("accident") || comment.Contains("unsafe"))
                        icon = "🚧";
                    else if (comment.Contains("safe") || review.SafetyRating >= 4)
                        icon = "🟢";
                    else if (comment.Contains("crowd") || review.SafetyRating == 3)
                        icon = "⚠️";
                    else if (comment.Contains("festival") || comment.Contains("event"))
                        icon = "🎉";

                    var pin = new Pin
                    {
                        Label = $"💬{icon}", // ✅ cleaner look
                        Address = review.LocationName,
                        Type = PinType.Place,
                        Location = new Location(review.Latitude, review.Longitude),
                    };

                    pin.MarkerClicked += async (s, e) =>
                    {
                        e.HideInfoWindow = true;

                        await DisplayAlert(
                            $"{icon} {review.LocationName}",
                            $"⭐ {review.StarDisplay}\n\n" +
                            $"{review.Comment}\n\n" +
                            $"🛡️ {review.StatusText}\n" +
                            $"📅 {review.Date:MMM dd, yyyy}",
                            "Close"
                        );
                    };

                    SafetyMap.Pins.Add(pin);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeMap] Error loading reviews: {ex.Message}");
            }
        }



        public async Task RefreshSafetyReviews()
        {
            var reviewPins = SafetyMap.Pins
                .Where(p => p.Type == PinType.Place)
                .ToList();

            foreach (var pin in reviewPins)
            {
                SafetyMap.Pins.Remove(pin);
            }

            await LoadSafetyReviews();
        }
    }
}