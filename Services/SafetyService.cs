using Safarion.Models;

namespace Safarion.Services
{
    public class SafetyService
    {
        private readonly DatabaseService _dbService;
        private readonly IVoiceService? _voiceService;

        private bool _isShakeActive = false;
        private bool _isProcessingSOS = false;
        private DateTime _lastShakeTime = DateTime.MinValue;

        public SafetyService()
        {
            _dbService = new DatabaseService();

            try
            {
                _voiceService = App.Current?.Handler?.MauiContext?.Services.GetService<IVoiceService>();
            }
            catch
            {
                _voiceService = null;
            }
        }

        // =====================================================
        // VOICE SOS
        // =====================================================

        public async Task ListenForHelpCommand()
        {
            if (_voiceService == null)
            {
                await ShowAlert("Voice Not Supported", "Voice detection not available on this device.");
                return;
            }

            var status = await Permissions.RequestAsync<Permissions.Microphone>();

            if (status != PermissionStatus.Granted)
            {
                await ShowAlert("Permission Required", "Microphone permission is required for voice SOS.");
                return;
            }

            _voiceService.OnTextRecognized -= HandleVoiceResult;
            _voiceService.OnListeningError -= HandleVoiceError;

            _voiceService.OnTextRecognized += HandleVoiceResult;
            _voiceService.OnListeningError += HandleVoiceError;

            _voiceService.StartListening();
        }

        private async void HandleVoiceResult(object? sender, string spokenText)
        {
            await ProcessVoiceCommand(spokenText);
        }

        private void HandleVoiceError(object? sender, string error)
        {
            System.Diagnostics.Debug.WriteLine($"Voice error: {error}");
        }

        private async Task ProcessVoiceCommand(string spokenText)
        {
            if (string.IsNullOrWhiteSpace(spokenText))
                return;

            string text = spokenText.ToLower();

            // --- NEW: PHASE 2 MULTILINGUAL VOICE TRIGGERS ---
            // 1. Check the phone's memory for the user's language
            string userLanguage = Preferences.Default.Get("AppLanguage", "English");
            bool isEmergency = false;

            // 2. Dynamically change the "Ears" based on the language
            switch (userLanguage)
            {
                case "Spanish":
                    isEmergency = text.Contains("ayuda") || text.Contains("socorro") || text.Contains("emergencia") || text.Contains("sos");
                    break;

                case "Hindi":
                    // Added Hindi script just in case!
                    isEmergency = text.Contains("bachao") || text.Contains("madad") ||
                                  text.Contains("बचाओ") || text.Contains("मदद") ||
                                  text.Contains("emergency") || text.Contains("sos");
                    break;

                case "Malayalam":
                    // NEW: Added actual Malayalam script!
                    isEmergency = text.Contains("rakshikku") || text.Contains("sahayikku") ||
                                  text.Contains("rakshikkoo") || text.Contains("രക്ഷിക്കൂ") ||
                                  text.Contains("സഹായിക്കൂ") || text.Contains("sos");
                    break;

                case "French":
                    isEmergency = text.Contains("au secours") || text.Contains("aidez-moi") || text.Contains("urgence") || text.Contains("sos");
                    break;

                default: // English fallback
                    isEmergency = text.Contains("help") || text.Contains("save me") || text.Contains("emergency") || text.Contains("sos");
                    break;
            }

            // 3. If any of the native trigger words are spoken, fire the SOS!
            if (isEmergency)
            {
                TriggerHaptic();
                await TriggerSOS();
            }
            // ------------------------------------------------
        }

        // =====================================================
        // SHAKE DETECTION
        // =====================================================

        public async void ToggleShakeDetection(bool enable)
        {
            try
            {
                if (enable && !_isShakeActive)
                {
                    if (Accelerometer.Default.IsSupported)
                    {
                        Accelerometer.Default.ReadingChanged += OnShakeDetected;
                        Accelerometer.Default.Start(SensorSpeed.Game);
                        _isShakeActive = true;

                        await GetCurrentLocation(); // warm-up GPS
                    }
                }
                else if (!enable && _isShakeActive)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= OnShakeDetected;
                    _isShakeActive = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shake detection error: {ex.Message}");
            }
        }

        private async void OnShakeDetected(object? sender, AccelerometerChangedEventArgs e)
        {
            if (_isProcessingSOS) return;

            var data = e.Reading;

            double gForce =
                Math.Sqrt(
                    data.Acceleration.X * data.Acceleration.X +
                    data.Acceleration.Y * data.Acceleration.Y +
                    data.Acceleration.Z * data.Acceleration.Z);

            if (gForce > 2.5 && (DateTime.Now - _lastShakeTime).TotalSeconds > 2)
            {
                _lastShakeTime = DateTime.Now;

                TriggerHaptic();

                await TriggerSOS();
            }
        }

        // =====================================================
        // MAIN SOS
        // =====================================================

        public async Task TriggerSOS()
        {
            if (_isProcessingSOS)
                return;

            _isProcessingSOS = true;

            try
            {
                var contacts = await _dbService.GetContactsAsync();

                if (contacts.Count == 0)
                {
                    await ShowAlert("No Contacts", "Please add emergency contacts in your profile.");
                    return;
                }

                var location = await GetCurrentLocation();

                string mapsLink = location != null
                    ? $"https://maps.google.com/?q={location.Latitude},{location.Longitude}"
                    : "Location unavailable";

                var user = await _dbService.GetUserAsync();
                string userName = user?.FullName ?? "Safarion User";

                string message =
                    $"🚨 EMERGENCY ALERT 🚨\n\n" +
                    $"{userName} needs help.\n\n" +
                    $"Location:\n{mapsLink}\n\n" +
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                    $"Sent via Safarion Safety App.";

                foreach (var contact in contacts)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(contact.Phone))
                        {
                            await Sms.ComposeAsync(
                                new SmsMessage(message, new[] { contact.Phone })
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SMS error: {ex.Message}");
                    }
                }

                await ShowAlert("SOS Sent", $"Emergency alerts prepared for {contacts.Count} contact(s).");
            }
            catch (Exception ex)
            {
                await ShowAlert("SOS Failed", ex.Message);
            }
            finally
            {
                _isProcessingSOS = false;
            }
        }

        // =====================================================
        // LOCATION
        // =====================================================

        private async Task<Location?> GetCurrentLocation()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (status != PermissionStatus.Granted)
                        return null;
                }

                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.High,
                        TimeSpan.FromSeconds(10));

                    location = await Geolocation.GetLocationAsync(request);
                }

                return location;
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        // FEEDBACK HELPERS
        // =====================================================

        private void TriggerHaptic()
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.LongPress);
            }
            catch { }
        }

        private async Task ShowAlert(string title, string message)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "OK");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"{title}: {message}");
                }
            });
        }
    }
}