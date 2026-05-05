using Safarion.Services;
using ZXing.Net.Maui;

namespace Safarion.Views
{
    public partial class QrCodePage : ContentPage
    {
        private readonly DatabaseService _dbService;

        public QrCodePage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await GenerateQrData();
        }

        private async Task GenerateQrData()
        {
            // 1. Fetch User Data
            var user = await _dbService.GetUserAsync();
            var contacts = await _dbService.GetContactsAsync();

            if (user == null) return;

            // 2. Format the Data String (This is what people see when they scan)
            string data = $"--- SAFARION MEDICAL ID ---\n" +
                          $"Name: {user.FullName}\n" +
                          $"Blood: {user.BloodGroup}\n" +
                          $"DOB: {user.Dob}\n" +
                          $"Allergies: {user.Allergies}\n" +
                          $"Meds: {user.Medications}\n" +
                          $"--------------------------\n" +
                          $"EMERGENCY CONTACTS:\n";

            foreach (var c in contacts)
            {
                data += $"{c.Name} ({c.Relationship}): {c.Phone}\n";
            }

            // 3. Feed it to the QR Generator
            QrCodeView.Value = data;
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}