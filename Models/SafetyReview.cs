// SafetyReview.cs

namespace Safarion.Models
{
    public class SafetyReview
    {
        public string StatusText => CategoryBadge;
        public bool IsLocationVerified { get; set; } = false;
        public string LocationLabel =>
    $"Location: {LocationName}";

        public string ReasonLabel =>
            $"Reason: {Comment}";
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string LocationName { get; set; } = "";
        public int SafetyRating { get; set; } = 5;
        public string Comment { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string CreatedBy { get; set; } = "Anonymous Traveler";
        public string Category { get; set; } = "General";

        public int Likes { get; set; } = 0;
        public int Dislikes { get; set; } = 0;

        public string UserName => CreatedBy;
        public string UserInitial => "A";

        public string StarDisplay =>
            new string('★', SafetyRating) +
            new string('☆', 5 - SafetyRating);

        public Color StarColor => Color.FromArgb("#FBC02D");

        public Color StatusColor =>
            SafetyRating < 3 ? Colors.Red :
            SafetyRating < 4 ? Colors.Orange :
            Colors.Green;

        public string CategoryBadge => Category switch
        {
            "Danger" => "🚫 DANGER",
            "Caution" => "⚠️ CAUTION",
            "Safe" => "✅ SAFE",
            "Food" => "🍜 FOOD",
            "Interesting Spot" => "📍 SPOT",
            "Festival / Event" => "🎉 FESTIVAL",
            _ => "💬 GENERAL"
        };
    }
}