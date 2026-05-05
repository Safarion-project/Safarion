using Safarion.Services;
using System.Collections.ObjectModel;
using Safarion.Models;
namespace Safarion.Views;
using Microsoft.Maui.Storage;

using System.Text.Json;
using Microsoft.Maui.Storage;

public partial class DashboardPage : ContentPage
{
    private readonly SafetyService _safetyService;
    private readonly DatabaseService _dbService;

    ObservableCollection<Destination> destinations =
        new ObservableCollection<Destination>();

    public DashboardPage()
    {
        InitializeComponent();
        _safetyService = new SafetyService(); // Load the safety logic
        _dbService = new DatabaseService();
        bool hasNewMessage = false;        // connect to your backend later
        bool hasFriendRequest = false;    // connect to backend

        LoadDestinations();

        DestinationList.ItemsSource = destinations;

        UpdateNotifications(hasNewMessage, hasFriendRequest);
    }

    // 1. Turn Sensor ON when page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Turn on sensors and update UI
        _safetyService.ToggleShakeDetection(true);
        await LoadUserGreeting();
        UpdateDate();

        // 2. Check if it's the first time the app is launched
        bool isFirstLaunch = Preferences.Get("IsFirstLaunch", true);

        if (isFirstLaunch)
        {
            // Set it to false so it never auto-pops up again
            Preferences.Set("IsFirstLaunch", false);

            // Show the guide as a Modal
            await Navigation.PushModalAsync(new NavigationPage(new HowToUsePage()));

            if (Navigation != null)
            {
            }
        }
    }

    private async Task LoadUserGreeting()
    {
        var user = await _dbService.GetUserAsync();

        string userName = "Traveler";

        if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
        {
            userName = user.FullName;
            ProfileInitialLabel.Text = user.FullName[0].ToString().ToUpper();
        }
        else
        {
            ProfileInitialLabel.Text = "T";
        }

        // ⏰ Time-based greeting
        int hour = DateTime.Now.Hour;
        string greeting;

        if (hour >= 23 || hour < 4)
        {
            greeting = $"Hi there {userName}, still up for exploration? 🌙";
        }
        else if (hour >= 4 && hour < 12)
        {
            greeting = $"Morning~ {userName}, shall we get started? ☀️";
        }
        else if (hour >= 12 && hour < 17)
        {
            greeting = $"Survived morning! Let's take a little~ detour, {userName} 🍦";
        }
        else
        {
            greeting = $"Yo {userName}, perfect time for the evening rush! 🌆";
        }

        GreetingLabel.Text = greeting;

        // 🖼 Profile picture logic
        var photoPath = Preferences.Get("ProfilePhotoPath", null);

        if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
        {
            ProfileImage.Source = ImageSource.FromFile(photoPath);
            ProfileImage.IsVisible = true;
            ProfileInitialLabel.IsVisible = false;
        }
        else
        {
            ProfileImage.IsVisible = false;
            ProfileInitialLabel.IsVisible = true;
        }
    }

    private void UpdateDate()
    {
        DateLabel.Text = DateTime.Now.ToString("MMM d, yyyy");
    }

    // 2. Turn Sensor OFF when leaving page (Saves Battery)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _safetyService.ToggleShakeDetection(false);
    }

    // --- SOS BUTTON LOGIC ---
    private async void OnSOSTapped(object sender, EventArgs e)
    {
        // 1. Visual Feedback (Button Shake/Flash)
        // (We can add animation later, for now let's just trigger logic)

        bool confirm = await DisplayAlert("⚠️ SOS EMERGENCY", "Are you sure you want to trigger an SOS Alert?", "YES, HELP!", "Cancel");

        if (confirm)
        {
            // 2. Call the Safety Service
            await _safetyService.TriggerSOS();
        }
    }

    // Event Handler for Map Button
    private async void OnMapTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SafeMapPage());
    }

    // Event Handler for Profile Button
    private async void OnProfileTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Border border)
            {
                await border.ScaleTo(0.9, 50);
                await border.ScaleTo(1.0, 50);
            }

            var action = await DisplayActionSheet(
                "Profile Options",
                "Cancel",
                null,
                "👤 View Profile",
                "📸 Change Profile Picture",
                "🔗 Show Medical QR Code",
                "📊 Travel Stats"
            );

            switch (action)
            {
                case "👤 View Profile":
                    await NavigateToProfile();
                    break;
                case "📸 Change Profile Picture":
                    await ChangeProfilePicture();
                    break;
                case "🔗 Show Medical QR Code":
                    await Navigation.PushAsync(new QrCodePage());
                    break;
                case "📊 Travel Stats":
                    await ShowTravelStats();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Profile menu error: {ex.Message}");
            await NavigateToProfile();
        }
    }

    private async Task NavigateToProfile()
    {
        try
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }
        catch
        {
            try
            {
                await Navigation.PushAsync(new ProfilePage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Navigation failed: {ex.Message}");
                await DisplayAlert("Error", "Could not open profile page", "OK");
            }
        }
    }

    private async Task ChangeProfilePicture()
    {
        try
        {
            var action = await DisplayActionSheet(
                "Profile Picture",
                "Cancel",
                null,
                "Add / Change Photo",
                "Remove Photo"
            );

            var fileName = $"profile_photo_{DateTime.Now.Ticks}.jpg";
            var photoPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            Preferences.Set("ProfilePhotoPath", photoPath);

            switch (action)
            {
                case "Add / Change Photo":
                    var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = "Select Profile Picture"
                    });

                    if (photo == null) return;

                    // FIX: Clear the current source to force a refresh later
                    ProfileImage.Source = null;

                    using (var stream = await photo.OpenReadAsync())
                    using (var newStream = File.Create(photoPath))
                    {
                        await stream.CopyToAsync(newStream);
                    }
                    break;

                case "Remove Photo":
                    ProfileImage.Source = null;
                    if (File.Exists(photoPath))
                    {
                        File.Delete(photoPath);
                    }
                    break;
            }

            // Refresh UI
            await LoadUserGreeting();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Profile photo error: {ex.Message}");
            await DisplayAlert("Error", "Something went wrong.", "OK");
        }
    }

   
    private async Task ShowTravelStats()
    {
        try
        {
            var memories = await DiaryService.LoadMemories();
            var reviews = await ReviewService.GetReviews();

            int totalTrips = memories.Count;
            int totalReviews = reviews.Count;

            // Known country keywords to detect if user specified a country in their memory titles
            var knownCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "india", "france", "japan", "usa", "uk", "germany", "italy", "spain",
            "thailand", "singapore", "dubai", "uae", "australia", "canada", "switzerland",
            "finland", "iceland", "bali", "indonesia", "malaysia", "nepal", "sri lanka",
            "maldives", "portugal", "greece", "turkey", "mexico", "brazil", "south korea"
            // Add more as needed
        };

            var mentionedCountries = memories
                .Select(m => (m.Title + " " + m.Description).ToLower())
                .SelectMany(text => knownCountries.Where(country => text.Contains(country)))
                .Distinct()
                .ToList();

            // If no country is found in any memory, default to 1 (home country)
            int countriesVisited = mentionedCountries.Count > 0 ? mentionedCountries.Count : 1;

            string message = $"🌍 Places Visited: {countriesVisited}\n" +
                            $"✈️ Total Trips: {totalTrips}\n" +
                            $"⭐ Reviews Posted: {totalReviews}\n\n" +
                            $"Keep exploring safely! 🎒";

            await DisplayAlert("Your Travel Journey", message, "Awesome!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Dashboard] Stats error: {ex.Message}");
            await DisplayAlert("Stats", "Travel stats coming soon!", "OK");
        }
    }

    private async void OnSanitizerTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Friend Requests", "Open friend requests here.", "OK");
    }

    private async void OnVoiceTapped(object sender, EventArgs e)
    {
        // Trigger the listening logic
        await _safetyService.ListenForHelpCommand();
    }



    private async void OnDestinationTapped(object sender, EventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext is Destination destination)
        {
            string url = $"https://www.tripadvisor.com/Search?q={destination.Name}+tour";

            await Launcher.Default.OpenAsync(url);
        }
    }

    void SaveDestinations()
    {
        string json = JsonSerializer.Serialize(destinations);
        Preferences.Set("destinations", json);
    }

    private async void OnAddDestination(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync(
            "Add Destination",
            "Enter location name:");

        if (string.IsNullOrWhiteSpace(name))
            return;

        string imageUrl = $"https://source.unsplash.com/featured/?{name}";

        destinations.Add(new Destination
        {
            Name = name,
            Image = imageUrl,
            TravelUrl = $"https://www.tripadvisor.com/Search?q={name}+tour"
        });

        SaveDestinations();
    }

    string GetDefaultImage(string place)
    {
        place = place.ToLower();

        if (place.Contains("switzerland"))
            return "switzerland.png";

        if (place.Contains("japan"))
            return "japan.png";

        if (place.Contains("dubai"))
            return "dubai.png";

        if (place.Contains("iceland"))
            return "iceland.png";

        return "default_destination.png";
    }

    void LoadDestinations()
    {
        if (Preferences.ContainsKey("destinations"))
        {
            string json = Preferences.Get("destinations", "");

            var saved = JsonSerializer.Deserialize<List<Destination>>(json);

            if (saved != null)
            {
                destinations.Clear();
                foreach (var d in saved)
                    destinations.Add(d);
            }
        }
        else
        {
            destinations = new ObservableCollection<Destination>
        {
            new Destination
            {
                Name="Paris",
                Image="paris.png",
                TravelUrl="https://www.makemytrip.com/holidays-international/paris-vacation-tour-packages.html"
            },

            new Destination
            {
                Name="Finland",
                Image="finland.png",
                TravelUrl="https://www.thomascook.in/holidays/international-tour-packages/finland-tour-packages"
            },

            new Destination
            {
                Name="Thailand",
                Image="thailand.png",
                TravelUrl="https://www.makemytrip.com/holidays-international/thailand-vacation-tour-packages.html"
            }
        };
        }

        
    }

    private async void OnRemoveDestination(object sender, EventArgs e)
    {
        if (sender is Button btn &&
            btn.BindingContext is Destination destination)
        {
            bool confirm = await DisplayAlert(
                "Remove Destination",
                $"Remove {destination.Name}?",
                "Yes",
                "Cancel");

            if (confirm)
            {
                destinations.Remove(destination);
                SaveDestinations();
            }
        }
    }

    private async void OnReviewsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SafetyReviewsPage());
    }

    bool isExpanded = false;

    // "Read More" toggle logic for the about section

    private void OnReadMoreTapped(object sender, EventArgs e)
    {
        if (!isExpanded)
        {
            AboutTextLabel.MaxLines = -1; // Show full text
            ReadMoreLabel.Text = "Show less";
            isExpanded = true;
        }
        else
        {
            AboutTextLabel.MaxLines = 10; // Collapse back
            ReadMoreLabel.Text = "Read full information...";
            isExpanded = false;
        }
    }

    // --- MENU LOGIC ---
    private async void OnMenuClicked(object sender, EventArgs e)
    {
        // Add "My Friends 👥" to the options
        string action = await DisplayActionSheet("Menu", "Cancel", null,
            "My Friends 👥", "Friend Requests 📩", "Edit Profile ✏️");

        if (action == "Friend Requests 📩")
        {
            await Navigation.PushAsync(new FriendRequestsPage());
        }
        else if (action == "My Friends 👥")
        {
            // Open the new Friends List
            await Navigation.PushAsync(new FriendsListPage());
        }
        else if (action == "Edit Profile ✏️")
        {
            string result = await DisplayPromptAsync("Edit Profile", "Enter new name:", "Save", "Cancel", placeholder: Preferences.Get("UserName", ""));
            if (!string.IsNullOrWhiteSpace(result))
            {
                Preferences.Set("UserName", result);
                await DisplayAlert("Saved", "Your name has been updated.", "OK");
            }
        }

    }

    public void UpdateNotifications(bool hasMessage, bool hasFriendRequest)
    {
        if (hasFriendRequest)
        {
            NotificationBadge.BackgroundColor = Colors.Red;
            NotificationBadge.IsVisible = true;
        }
        else if (hasMessage)
        {
            NotificationBadge.BackgroundColor = Colors.Green;
            NotificationBadge.IsVisible = true;
        }
        else
        {
            NotificationBadge.IsVisible = false;
        }
    }
}