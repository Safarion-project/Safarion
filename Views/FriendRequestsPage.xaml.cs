using Safarion.Services;

namespace Safarion.Views
{
    public partial class FriendRequestsPage : ContentPage
    {
        public FriendRequestsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRequests();
        }

        private async Task LoadRequests()
        {
            var requests = await RealtimeTravelerService.GetFriendRequests();
            RequestsList.ItemsSource = requests;
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var request = (FriendRequest)button.CommandParameter;

            await RealtimeTravelerService.AcceptFriendRequest(request.SenderId, request.SenderName);
            await DisplayAlert("Success", $"You are now friends with {request.SenderName}!", "OK");
            await LoadRequests(); // Refresh list
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var request = (FriendRequest)button.CommandParameter;

            await RealtimeTravelerService.DeclineFriendRequest(request.SenderId);
            await LoadRequests(); // Refresh list
        }
    }
}