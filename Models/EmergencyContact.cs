using SQLite;

namespace Safarion.Models
{
    public class EmergencyContact
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Who does this contact belong to? (Not strictly needed for single user app, but good practice)
        public int UserProfileId { get; set; }

        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Relationship { get; set; } // e.g. "Mom", "Brother"
    }
}