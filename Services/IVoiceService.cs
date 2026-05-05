namespace Safarion.Services
{
    public interface IVoiceService
    {
        void StartListening();
        event EventHandler<string> OnTextRecognized;
        // Renamed from 'OnError' to 'OnListeningError' to fix the conflict
        event EventHandler<string> OnListeningError;
    }
}