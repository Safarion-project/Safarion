using Safarion.Models;
using Safarion.Services;

namespace Safarion.Views
{
    public partial class AddMemoryPage : ContentPage
    {
        private TravelMemory _currentMemory;
        private List<string> _tempImagePaths = new();
        private Location? _selectedLocation = null;

        // Constructor for NEW Memory
        public AddMemoryPage()
        {
            InitializeComponent();
            _currentMemory = new TravelMemory();
        }

        // Constructor for EDITING Memory
        public AddMemoryPage(TravelMemory memoryToEdit)
        {
            InitializeComponent();
            _currentMemory = memoryToEdit;
            LoadExistingData();
        }

        private void LoadExistingData()
        {
            TitleEntry.Text = _currentMemory.Title;
            DescriptionEditor.Text = _currentMemory.Description;
            StartDatePicker.Date = _currentMemory.StartDate;
            ReturnDatePicker.Date = _currentMemory.ReturnDate;
            PrivacySwitch.IsToggled = _currentMemory.IsPublic;

            foreach (var path in _currentMemory.ImagePaths)
            {
                AddImageToView(path);
            }

            // Load location if exists
            if (_currentMemory.Latitude != 0 && _currentMemory.Longitude != 0)
            {
                _selectedLocation = new Location(_currentMemory.Latitude, _currentMemory.Longitude);
                UpdateLocationDisplay();
            }
        }

        // NEW: Privacy toggle handler
        private void OnPrivacyToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
            {
                PrivacyDescription.Text = "?? Public - Friends can see this memory";
                PrivacyDescription.TextColor = Color.FromArgb("#4CAF50");
            }
            else
            {
                PrivacyDescription.Text = "?? Private - Only you can see this memory";
                PrivacyDescription.TextColor = Colors.Gray;
            }
        }

        // NEW: Add current location
        private async void OnAddLocationClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    LocationButton.Text = "Getting location...";
                    LocationButton.IsEnabled = false;

                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    _selectedLocation = await Geolocation.Default.GetLocationAsync(request);

                    if (_selectedLocation != null)
                    {
                        UpdateLocationDisplay();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Could not get location. Try again.", "OK");
                    }

                    LocationButton.Text = "Update Location";
                    LocationButton.IsEnabled = true;
                }
                else
                {
                    await DisplayAlert("Permission Needed", "Please enable location permission to add location to memories.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                await DisplayAlert("Error", "Could not access location.", "OK");
                LocationButton.Text = "Add Current Location";
                LocationButton.IsEnabled = true;
            }
        }

        private void UpdateLocationDisplay()
        {
            if (_selectedLocation != null)
            {
                LocationLabel.Text = $"?? {_selectedLocation.Latitude:F4}, {_selectedLocation.Longitude:F4}";
                LocationLabel.IsVisible = true;
                LocationButton.Text = "? Location Added";
                LocationButton.BackgroundColor = Color.FromArgb("#4CAF50");
            }
        }

        private async void OnPickPhotosClicked(object sender, EventArgs e)
        {
            try
            {
                // FIX: Use 'FilePicker' (not MediaPicker) to select multiple images
                var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Select Trip Photos",
                    FileTypes = FilePickerFileType.Images
                });

                if (results != null)
                {
                    foreach (var photo in results)
                    {
                        // 1. Create a permanent file path in the app's hidden storage
                        string newFile = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);

                        // 2. Copy the photo there (so it persists)
                        using var stream = await photo.OpenReadAsync();
                        using var newStream = File.OpenWrite(newFile);
                        await stream.CopyToAsync(newStream);

                        // 3. Show it on screen
                        AddImageToView(newFile);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not upload photos: {ex.Message}", "OK");
            }
        }

        private void AddImageToView(string path)
        {
            _tempImagePaths.Add(path);
            var img = new Image { Source = path, HeightRequest = 100, WidthRequest = 100, Aspect = Aspect.AspectFill };
            ImagesPanel.Children.Insert(0, img); // Add before the + button
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await DisplayAlert("Required", "Please enter a title for your memory", "OK");
                return;
            }

            // Update the object
            _currentMemory.Title = TitleEntry.Text;
            _currentMemory.Description = DescriptionEditor.Text;
            _currentMemory.StartDate = StartDatePicker.Date;
            _currentMemory.ReturnDate = ReturnDatePicker.Date;
            _currentMemory.IsPublic = PrivacySwitch.IsToggled;

            // Save location if added
            if (_selectedLocation != null)
            {
                _currentMemory.Latitude = _selectedLocation.Latitude;
                _currentMemory.Longitude = _selectedLocation.Longitude;
            }

            // Only update images if user added new ones, else keep old ones
            if (_tempImagePaths.Count > 0)
                _currentMemory.ImagePaths = _tempImagePaths;

            await DiaryService.SaveMemoryAsync(_currentMemory);

            string visibility = _currentMemory.IsPublic ? "public" : "private";
            await DisplayAlert("Saved", $"Memory saved as {visibility}!", "OK");

            await Navigation.PopAsync();
        }
    }
}