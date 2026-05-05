using System.Text.Json.Serialization;

namespace Safarion.Models
{
    public class TravelMemory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime ReturnDate { get; set; } = DateTime.Now;

        // NEW: Privacy control
        public bool IsPublic { get; set; } = false; // Default to private

        // NEW: Who created this memory (for multi-user features)
        public string CreatedBy { get; set; } = "Me"; // Username or UserId

        // NEW: Location data for map display
        public double Latitude { get; set; } = 0.0;
        public double Longitude { get; set; } = 0.0;

        // Supports multiple photos
        public List<string> ImagePaths { get; set; } = new List<string>();

        // Helper for the UI to show just the first "Cover Image"
        [JsonIgnore]
        public string CoverImage => ImagePaths.Count > 0 ? ImagePaths[0] : "upload_placeholder.png";

        // Helper for privacy icon in UI
        [JsonIgnore]
        public string PrivacyIcon => IsPublic ? "🌍" : "🔒";

        [JsonIgnore]
        public string PrivacyText => IsPublic ? "Public" : "Private";

        [JsonIgnore]
        public Color PrivacyColor => IsPublic ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800");
    }
}