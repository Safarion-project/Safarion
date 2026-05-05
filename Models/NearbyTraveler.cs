using Microsoft.Maui.Devices.Sensors;
using System.Text.Json.Serialization; // Fixes "JsonIgnore" error

namespace Safarion.Models
{
    public class NearbyTraveler
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        // FIX: Firebase needs simple numbers
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Helper: Converts the numbers back to a Location object for the Map
        [JsonIgnore]
        public Location Position => new Location(Latitude, Longitude);
    }
}