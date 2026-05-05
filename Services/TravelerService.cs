using Safarion.Models;

namespace Safarion.Services
{
    public static class TravelerService
    {
        private static readonly Random _random = new Random();

        // Generates 5 fake people around YOUR location
        public static List<NearbyTraveler> GetSimulatedTravelers(Location myLocation)
        {
            var list = new List<NearbyTraveler>();
            var names = new[] { "Alice", "Rahul", "Sarah", "Arjun", "Maya" };
            var statuses = new[] { "Taking photos 📸", "Solo Traveler 🎒", "Drinking Coffee ☕", "Exploring 🗺️", "Walking 🚶" };

            for (int i = 0; i < 5; i++)
            {
                // Create a random offset (roughly 100-500 meters)
                double latOffset = (_random.NextDouble() * 0.006) - 0.003;
                double lngOffset = (_random.NextDouble() * 0.006) - 0.003;

                list.Add(new NearbyTraveler
                {
                    Name = names[i],
                    Status = statuses[i],
                    // FIX: Set Latitude & Longitude directly (Position is read-only)
                    Latitude = myLocation.Latitude + latOffset,
                    Longitude = myLocation.Longitude + lngOffset
                });
            }

            return list;
        }
    }
}