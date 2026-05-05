using Safarion.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using Firebase.Database.Streaming;
using Newtonsoft.Json;

namespace Safarion.Views
{
    public partial class TravelerChatPage : ContentPage
    {
        private string _otherUserId;

        // This acts as the "off switch" for the real-time listener
        private IDisposable _chatSubscription;
        
        public ObservableCollection<TravelerMessage> Messages { get; set; } = new ObservableCollection<TravelerMessage>();

        public TravelerChatPage(string userId, string userName)
        {
            InitializeComponent();
            _otherUserId = userId;
            Title = $"Chat with {userName}";
            MessagesList.ItemsSource = Messages;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Messages.Clear();
            ListenForMessages();
        }

        // CRITICAL FIX: Close the WebSocket connection when you hit the back button!
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _chatSubscription?.Dispose();
        }

        private void ListenForMessages()
        {
            var observable = RealtimeTravelerService.ListenToChat(_otherUserId);

            // Assign the listener to our variable so we can close it later
            _chatSubscription = observable.Subscribe(evt =>
            {
                if (evt.Object != null && evt.EventType == FirebaseEventType.InsertOrUpdate)
                {
                    try
                    {
                        // 1. Use Newtonsoft to cleanly handle the Firebase object
                        string jsonString = JsonConvert.SerializeObject(evt.Object);
                        var data = JsonConvert.DeserializeObject<TravelerMessage>(jsonString);

                        if (data == null) return;

                        // 2. DIAGNOSTIC CHECK: If text is missing, don't hide it! Show a warning.
                        if (string.IsNullOrWhiteSpace(data.Text))
                        {
                            data.Text = "⚠️ [Text missing: Check Firebase property name]";
                        }

                        string myId = RealtimeTravelerService.GetMyUserId();
                        data.IsUser = (data.SenderId == myId);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Prevent duplicates from showing up on the screen
                            bool messageExists = Messages.Any(m => m.Text == data.Text && m.Time == data.Time);

                            if (!messageExists)
                            {
                                Messages.Add(data);

                                // Scroll to the very bottom to see the newest text
                                try { MessagesList.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true); } catch { }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // If it fails, it will now print to your Output window so you can see why!
                        Console.WriteLine($"CHAT ERROR: {ex.Message}");
                    }
                }
            });
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MsgEntry.Text)) return;

            string messageToSend = MsgEntry.Text;
            MsgEntry.Text = ""; // Clear the box immediately so it feels fast

            await RealtimeTravelerService.SendMessage(_otherUserId, messageToSend);
        }
    }

    public class TravelerMessage
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }

        public bool IsUser { get; set; }
        public bool IsNotUser => !IsUser;

        public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;
        public Color BackgroundColor => IsUser ? Color.FromArgb("#00E676") : Color.FromArgb("#E0E0E0");
        public Color TextColor => IsUser ? Colors.Black : Color.FromArgb("#212121");
    }
}