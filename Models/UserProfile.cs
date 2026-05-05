using SQLite;

namespace Safarion.Models
{
    public class UserProfile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // --- PERSONAL HEADER ---
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Dob { get; set; } // Date of Birth

        // --- MEDICAL TABLE DATA ---
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public string? Medications { get; set; } // Currently taking

        public bool IsSafetyModeEnabled { get; set; }
    }
}