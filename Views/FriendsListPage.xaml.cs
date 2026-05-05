using Safarion.Services;
using System.Collections.ObjectModel;
using Firebase.Database;
using Firebase.Database.Streaming; // Needed for EventType

namespace Safarion.Views
{
    public partial class FriendsListPage : ContentPage
    {
        public ObservableCollection<FriendRequest> Friends { get; set; } = new ObservableCollection<FriendRequest>();

        public FriendsListPage()
        {
            InitializeComponent();
            FriendsList.ItemsSource = Friends;
        }

        private IDisposable? _subscription;
        protected override void OnAppearing()
        {
            base.OnAppearing();
            SubscribeToFriends();
        }

        private async void SubscribeToFriends()
        {
            // ?? FIX: Prevent duplicate subscriptions
            _subscription?.Dispose();
            Friends.Clear();

            LoadingView.IsVisible = true;
            FriendsList.IsVisible = false;

            await Task.Delay(600);

            LoadingView.IsVisible = false;
            FriendsList.IsVisible = true;

            var observable = RealtimeTravelerService.ListenToFriends();

            _subscription = observable.Subscribe(evt =>
            {
                if (evt.Object == null) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    string userId = evt.Key;
                    string name = evt.Object.SenderName;

                    var existing = Friends.FirstOrDefault(x => x.SenderId == userId);

                    if (evt.EventType == FirebaseEventType.InsertOrUpdate)
                    {
                        if (existing == null)
                        {
                            Friends.Add(new FriendRequest
                            {
                                SenderId = userId,
                                SenderName = name,
                                Status = "Accepted"
                            });
                        }
                    }
                    else if (evt.EventType == FirebaseEventType.Delete)
                    {
                        if (existing != null)
                            Friends.Remove(existing);
                    }
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _subscription?.Dispose();
        }
        private async void OnViewDiaryClicked(object sender, EventArgs e)
        {
            if (sender is not Button btn || btn.CommandParameter is not FriendRequest friend)
                return;

            await Navigation.PushAsync(new FriendDiariesPage(friend.SenderName));
        }
        private async void OnChatClicked(object sender, EventArgs e)
        {
            if (sender is not Button btn || btn.CommandParameter is not FriendRequest friend)
                return;

            await Navigation.PushAsync(new TravelerChatPage(friend.SenderId, friend.SenderName));
        }

        private async void OnTrackClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SafeMapPage());
        }

        private async void OnFindClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SafeMapPage());
        }
    }
}