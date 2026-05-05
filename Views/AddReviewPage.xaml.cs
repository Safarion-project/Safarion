using Safarion.Models;
using Safarion.Services;

namespace Safarion.Views
{
    public partial class AddReviewPage : ContentPage
    {
        private SafetyReview _currentReview;
        private Location? _reviewLocation = null;

        // OLD LOGIC KEPT: Category mapping (UI -> Model)
        private readonly Dictionary<string, string> _categoryMap = new()
        {
            { "🚫 Danger",           "Danger"   },
            { "⚠️ Caution",          "Caution"  },
            { "✅ Safe",             "Safe"     },
            { "🍜 Food",             "Food"     },
            { "📍 Interesting Spot", "Spot"     },
            { "🎉 Festival / Event", "Festival" },
            { "💬 General",          "General"  }
        };

        // =====================================================
        // CONSTRUCTOR FOR NEW REVIEW
        // =====================================================
        public AddReviewPage()
        {
            InitializeComponent();

            _currentReview = new SafetyReview();

            // OLD LOGIC KEPT
            CategoryPicker.SelectedIndex = 6; // General

            // NEW FEATURE KEPT
            CaptureCurrentLocation();
        }

        // =====================================================
        // CONSTRUCTOR FOR EDITING REVIEW
        // =====================================================
        public AddReviewPage(SafetyReview existingReview)
        {
            InitializeComponent();

            _currentReview = existingReview;

            // Fill UI fields
            LocationEntry.Text = _currentReview.LocationName;
            RatingSlider.Value = _currentReview.SafetyRating;
            CommentEditor.Text = _currentReview.Comment;

            // Show delete button
            DeleteButton.IsVisible = true;

            // OLD FEATURE KEPT:
            // Restore category selection
            var entry = _categoryMap.FirstOrDefault(x => x.Value == _currentReview.Category);

            if (!string.IsNullOrEmpty(entry.Key))
                CategoryPicker.SelectedItem = entry.Key;
            else
                CategoryPicker.SelectedIndex = 6;

            // Existing GPS
            if (_currentReview.Latitude != 0 &&
                _currentReview.Longitude != 0)
            {
                _reviewLocation = new Location(
                    _currentReview.Latitude,
                    _currentReview.Longitude);
            }
        }

        // =====================================================
        // AUTO CAPTURE CURRENT GPS
        // =====================================================
        private async void CaptureCurrentLocation()
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10));

                _reviewLocation =
                    await Geolocation.Default.GetLocationAsync(request);

                if (_reviewLocation != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AddReview] GPS Captured: {_reviewLocation.Latitude}, {_reviewLocation.Longitude}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AddReview] GPS Error: {ex.Message}");
            }
        }

        // =====================================================
        // SUBMIT REVIEW
        // =====================================================
        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            // Validate location name
            if (string.IsNullOrWhiteSpace(LocationEntry.Text))
            {
                await DisplayAlert(
                    "Error",
                    "Please enter a location name",
                    "OK");
                return;
            }

            // NEW FEATURE: GPS Required
            if (_reviewLocation == null)
            {
                await DisplayAlert(
                    "GPS Error",
                    "We could not find your current location. Please enable GPS and grant permissions.",
                    "OK");
                return;
            }

            // ----------------------------------------
            // OLD DATA SAVE LOGIC KEPT
            // ----------------------------------------
            _currentReview.LocationName = LocationEntry.Text.Trim();
            _currentReview.SafetyRating = (int)RatingSlider.Value;
            _currentReview.Comment = CommentEditor.Text ?? "";
            _currentReview.Date = DateTime.Now;

            // OLD CATEGORY LOGIC KEPT
            if (CategoryPicker.SelectedItem is string pickerText &&
                _categoryMap.TryGetValue(pickerText, out string? category))
            {
                _currentReview.Category = category;
            }
            else
            {
                _currentReview.Category = "General";
            }

            // Save current GPS
            _currentReview.Latitude = _reviewLocation.Latitude;
            _currentReview.Longitude = _reviewLocation.Longitude;

            // NEW FEATURE
            _currentReview.IsLocationVerified = false;

            // ----------------------------------------
            // NEW FEATURE:
            // Verify user is near typed location
            // ----------------------------------------
            try
            {
                var locations =
                    await Geocoding.Default.GetLocationsAsync(
                        LocationEntry.Text);

                var targetLocation = locations?.FirstOrDefault();

                if (targetLocation != null)
                {
                    double distanceKm =
                        Location.CalculateDistance(
                            _reviewLocation,
                            targetLocation,
                            DistanceUnits.Kilometers);

                    // Within 500m
                    if (distanceKm <= 0.5)
                    {
                        _currentReview.IsLocationVerified = true;
                    }
                    else
                    {
                        await DisplayAlert(
                            "Verification Failed",
                            $"You are {Math.Round(distanceKm, 1)} km away from this place. You must be within 500 meters to post a verified review.",
                            "OK");

                        return;
                    }
                }
                else
                {
                    await DisplayAlert(
                        "Location Not Found",
                        "We could not find that place on the map. Please type a clearer name.",
                        "OK");
                    return;
                }
            }
            catch
            {
                await DisplayAlert(
                    "Network Error",
                    "Could not verify location. Please check internet connection.",
                    "OK");
                return;
            }

            // ----------------------------------------
            // SAVE TO DATABASE
            // ----------------------------------------
            await ReviewService.SaveReview(_currentReview);

            await DisplayAlert(
                "Saved",
                "Your verified safety review has been posted!",
                "OK");

            await Navigation.PopAsync();
        }

        // =====================================================
        // DELETE REVIEW
        // =====================================================
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Delete?",
                "Remove this review permanently?",
                "Yes",
                "No");

            if (confirm)
            {
                await ReviewService.DeleteReview(_currentReview.Id);
                await Navigation.PopAsync();
            }
        }
    }
}