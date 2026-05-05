using Android.Content;
using Android.Speech;
using Android.Runtime;
using Android.OS; // Added for Bundle
using Safarion.Services;

[assembly: Dependency(typeof(Safarion.Platforms.Android.VoiceService))]
namespace Safarion.Platforms.Android
{
    public class VoiceService : Java.Lang.Object, IVoiceService, IRecognitionListener
    {
        // Interface Events
        public event EventHandler<string>? OnTextRecognized;
        public event EventHandler<string>? OnListeningError; // Renamed matches Interface

        private SpeechRecognizer? _speechRecognizer;
        private Intent? _speechIntent;

        public void StartListening()
        {
            var activity = Platform.CurrentActivity;

            if (_speechRecognizer == null)
            {
                _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(activity);
                _speechRecognizer?.SetRecognitionListener(this);

                _speechIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                _speechIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                _speechIntent.PutExtra(RecognizerIntent.ExtraCallingPackage, activity?.PackageName);
            }

            _speechRecognizer?.StartListening(_speechIntent);
        }

        // --- ANDROID CALLBACKS (IRecognitionListener) ---

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (matches != null && matches.Count > 0)
            {
                OnTextRecognized?.Invoke(this, matches[0]);
            }
        }

        // FIX 1: This Method no longer clashes with the Event (because we renamed the event)
        public void OnError([GeneratedEnum] SpeechRecognizerError error)
        {
            OnListeningError?.Invoke(this, error.ToString());
        }

        // FIX 2: Added '@' before params to fix "Identifier expected" error
        public void OnReadyForSpeech(Bundle? @params) { }

        public void OnBeginningOfSpeech() { }

        public void OnRmsChanged(float rmsdB) { }

        public void OnBufferReceived(byte[]? buffer) { }

        public void OnEndOfSpeech() { }

        public void OnPartialResults(Bundle? partialResults) { }

        // FIX 3: Added '@' here too
        public void OnEvent(int eventType, Bundle? @params) { }
    }
}