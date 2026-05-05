using Safarion.Models;
using Safarion.Services;
using System.Collections.ObjectModel;

namespace Safarion.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly DatabaseService _dbService;
        private UserProfile? _currentUser;

        public ObservableCollection<EmergencyContact> Contacts { get; set; } = new();

        public ProfilePage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            ContactsList.ItemsSource = Contacts;

            // --- NEW: PHASE 2 LOAD LANGUAGE ---
            string savedLanguage = Preferences.Default.Get("AppLanguage", "English");
            LanguagePicker.SelectedItem = savedLanguage;
            // ----------------------------------
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Task.Delay(150); // smoother load
            await LoadData();
        }

        private async Task LoadData()
        {
            // Load profile
            _currentUser = await _dbService.GetUserAsync();

            if (_currentUser != null)
            {
                NameEntry.Text = _currentUser.FullName;
                EmailEntry.Text = _currentUser.Email;
                PhoneEntry.Text = _currentUser.Phone;
                DobEntry.Text = _currentUser.Dob;
                HeightEntry.Text = _currentUser.Height;
                WeightEntry.Text = _currentUser.Weight;
                BloodEntry.Text = _currentUser.BloodGroup;
                AllergiesEntry.Text = _currentUser.Allergies;
                MedsEntry.Text = _currentUser.Medications;
            }
            else
            {
                _currentUser = new UserProfile();
            }

            // Load profile photo
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

                if (!string.IsNullOrWhiteSpace(_currentUser?.FullName))
                    ProfileInitialLabel.Text = _currentUser.FullName.Trim()[0].ToString().ToUpper();
            }

            // Load contacts
            var contactList = await _dbService.GetContactsAsync();

            Contacts.Clear();
            foreach (var c in contactList)
                Contacts.Add(c);
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            _currentUser ??= new UserProfile();

            _currentUser.FullName = NameEntry.Text;
            _currentUser.Email = EmailEntry.Text;
            _currentUser.Phone = PhoneEntry.Text;
            _currentUser.Dob = DobEntry.Text;
            _currentUser.Height = HeightEntry.Text;
            _currentUser.Weight = WeightEntry.Text;
            _currentUser.BloodGroup = BloodEntry.Text;
            _currentUser.Allergies = AllergiesEntry.Text;
            _currentUser.Medications = MedsEntry.Text;

            await _dbService.SaveUserAsync(_currentUser);
            await DisplayAlert("Saved ✅", "Medical Profile Updated", "OK");
        }

        private async void OnAddContactClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewContactName.Text) ||
                string.IsNullOrWhiteSpace(NewContactPhone.Text))
            {
                await DisplayAlert("Error", "Name and Phone are required", "OK");
                return;
            }

            var newContact = new EmergencyContact
            {
                Name = NewContactName.Text,
                Phone = NewContactPhone.Text,
                Relationship = NewContactRel.Text
            };

            await _dbService.SaveContactAsync(newContact);
            Contacts.Add(newContact);

            NewContactName.Text = "";
            NewContactPhone.Text = "";
            NewContactRel.Text = "";
        }

        private async void OnDeleteContactClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is EmergencyContact contact)
            {
                bool confirm = await DisplayAlert("Remove?", $"Remove {contact.Name}?", "Yes", "No");

                if (confirm)
                {
                    await _dbService.DeleteContactAsync(contact);
                    Contacts.Remove(contact);
                }
            }
        }

        // 📸 View full profile photo
        private async void OnViewProfilePhotoTapped(object sender, TappedEventArgs e)
        {
            var photoPath = Preferences.Get("ProfilePhotoPath", null);

            if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
                await Navigation.PushAsync(new ProfilePhotoViewPage(photoPath));
            else
                await DisplayAlert("No Photo", "Set a profile photo first.", "OK");
        }

        // 📱 QR navigation
        private async void OnShowQrTapped(object sender, TappedEventArgs e)
            => await Navigation.PushAsync(new QrCodePage());

        private async void OnShowQrClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new QrCodePage());

        // 📘 Help page
        private async void OnHelpClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new HowToUsePage());

        // --- NEW: PHASE 2 SAVE LANGUAGE ---
        private async void OnLanguageChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedItem is string selectedLanguage)
            {
                // Instantly save the choice to the phone's local secure storage
                Preferences.Default.Set("AppLanguage", selectedLanguage);

                // Show a quick confirmation
                await DisplayAlert("Language Updated", $"Safarion will now assist you in {selectedLanguage}.", "OK");
            }
        }
    }
}