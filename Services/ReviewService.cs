// ReviewService.cs
// FULL UPDATED VERSION (replace old file)

using System.Text.Json;
using Safarion.Models;

namespace Safarion.Services
{
    public static class ReviewService
    {
        private static string FilePath =>
            Path.Combine(FileSystem.AppDataDirectory, "reviews.json");

        public static async Task<List<SafetyReview>> GetReviews()
        {
            if (!File.Exists(FilePath))
                return new List<SafetyReview>();

            string json = await File.ReadAllTextAsync(FilePath);

            var list =
                JsonSerializer.Deserialize<List<SafetyReview>>(json)
                ?? new List<SafetyReview>();

            // AUTO CLEANUP
            list = list
                .Where(r =>
                    (DateTime.Now - r.Date).TotalDays <= 30 &&
                    r.Dislikes < 5)
                .ToList();

            await SaveAllReviews(list);

            return list;
        }

        public static async Task SaveReview(SafetyReview review)
        {
            var list = await GetReviews();

            var existing =
                list.FirstOrDefault(x => x.Id == review.Id);

            if (existing != null)
            {
                int index = list.IndexOf(existing);
                list[index] = review;
            }
            else
            {
                list.Insert(0, review);
            }

            await SaveAllReviews(list);
        }

        public static async Task DeleteReview(string id)
        {
            var list = await GetReviews();

            var item =
                list.FirstOrDefault(x => x.Id == id);

            if (item != null)
                list.Remove(item);

            await SaveAllReviews(list);
        }

        // NEW: SAVE FULL LIST
        public static async Task SaveAllReviews(
            List<SafetyReview> reviews)
        {
            string json =
                JsonSerializer.Serialize(
                    reviews,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await File.WriteAllTextAsync(FilePath, json);
        }

        // NEW: LIKE
        public static async Task LikeReview(string id)
        {
            var list = await GetReviews();

            var item =
                list.FirstOrDefault(x => x.Id == id);

            if (item == null) return;

            item.Likes++;

            await SaveAllReviews(list);
        }

        // NEW: DISLIKE
        public static async Task DislikeReview(string id)
        {
            var list = await GetReviews();

            var item =
                list.FirstOrDefault(x => x.Id == id);

            if (item == null) return;

            item.Dislikes++;

            // AUTO DELETE IF 5 DISLIKES
            if (item.Dislikes >= 5)
                list.Remove(item);

            await SaveAllReviews(list);
        }
    }
}