using Safarion.Models;
using Safarion.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;

// ✅ NEW (for location + mic)
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace Safarion.Views
{
    public partial class ChatPage : ContentPage
    {
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();

        public ChatPage()
        {
            InitializeComponent();
            MessagesList.ItemsSource = Messages;

            AddAiMessage("Hello! I am Safarion 🧠.\n\nAsk me about:\n🍛 Local Food\n🎭 Cultural Tips\n🆘 Safety Advice");
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;

            string userText = MessageInput.Text;

            // ✅ Show user message
            Messages.Add(new ChatMessage { Text = userText, IsUser = true });
            MessageInput.Text = "";
            MessagesList.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);

            // ============================
            // 🚨 FAST SOS CHECK (UNCHANGED)
            // ============================
            if (userText.Contains("sos", StringComparison.OrdinalIgnoreCase) ||
                userText.Contains("help me", StringComparison.OrdinalIgnoreCase))
            {
                AddAiMessage("🚨 ACTIVATING EMERGENCY PROTOCOL.");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("⚠️ EMERGENCY", "Triggering Sirens & Sending Location...", "OK");
                });

                return;
            }

            // ============================
            // 🔍 CHECK LOCATION NEED
            // ============================
            bool needsLocation =
                userText.Contains("near me", StringComparison.OrdinalIgnoreCase) ||
                userText.Contains("nearby", StringComparison.OrdinalIgnoreCase) ||
                userText.Contains("around me", StringComparison.OrdinalIgnoreCase) ||
                userText.Contains("here", StringComparison.OrdinalIgnoreCase) ||
                userText.Contains("this place", StringComparison.OrdinalIgnoreCase);

            // ============================
            // ⚡ START LOCATION IN PARALLEL
            // ============================
            var locationTask = needsLocation ? GetUserLocationAsync() : Task.FromResult<Location?>(null);

            // ============================
            // 💬 SHOW TYPING (UX BOOST)
            // ============================
            var typingMsg = new ChatMessage { Text = "Typing...", IsUser = false };
            Messages.Add(typingMsg);

            // ============================
            // 📍 BUILD PROMPT (LIGHTWEIGHT)
            // ============================
            string finalPrompt = userText;

            if (needsLocation)
            {
                var location = await locationTask;

                if (location != null)
                {
                    // ✅ SHORT prompt → faster Gemini response
                    finalPrompt = $@"
User at {location.Latitude}, {location.Longitude}.
Answer like a friendly local guide. No bullets.

User: {userText}";
                }
                else
                {
                    finalPrompt = $@"
User asked for nearby suggestions but location unavailable.

User: {userText}";
                }
            }

            // ============================
            // 🌍 AI CALL
            // ============================
            string aiResponse = await GeminiService.GetResponse(finalPrompt);

            // ============================
            // 💬 REMOVE TYPING + SHOW RESULT
            // ============================
            Messages.Remove(typingMsg);
            AddAiMessage(aiResponse);
        }

        // ============================
        // 📍 FAST LOCATION METHOD
        // ============================
        private async Task<Location?> GetUserLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Default, // ⚡ better speed balance
                    TimeSpan.FromSeconds(5)
                );

                return await Geolocation.GetLocationAsync(request);
            }
            catch
            {
                return null;
            }
        }

        // ============================
        // 🎤 MIC INPUT (UNCHANGED + FIXED)
        // ============================
        private async void OnMicClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission", "Microphone permission is required.", "OK");
                    return;
                }

                var culture = new CultureInfo("en-IN");

                var result = await SpeechToText.Default.ListenAsync(
                    culture,
                    null,
                    CancellationToken.None
                );

                if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Text))
                {
                    MessageInput.Text = result.Text;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Voice Error", ex.Message, "OK");
            }
        }

        private void AddAiMessage(string text)
        {
            Messages.Add(new ChatMessage { Text = text, IsUser = false });
            MessagesList.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);
        }
    }

    // ============================
    // 💬 MESSAGE MODEL (UNCHANGED)
    // ============================
    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }

        public LayoutOptions Alignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;

        public Color BubbleColor => IsUser
            ? Color.FromArgb("#00E676")
            : Color.FromArgb("#E0E0E0");

        public Color TextColor => Colors.Black;
    }
}