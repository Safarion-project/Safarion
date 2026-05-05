using System.Text.Json;
using Safarion.Models;

namespace Safarion.Services
{
    public static class DiaryService
    {
        private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "diary.json");

        public static async Task<List<TravelMemory>> LoadMemories()
        {
            if (!File.Exists(FilePath)) return new List<TravelMemory>();
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<TravelMemory>>(json) ?? new List<TravelMemory>();
        }

        public static async Task SaveMemoryAsync(TravelMemory memory)
        {
            var list = await LoadMemories();

            // CHECK: Is this an Update or a New Entry?
            var existing = list.FirstOrDefault(m => m.Id == memory.Id);
            if (existing != null)
            {
                // UPDATE: Replace the old one
                int index = list.IndexOf(existing);
                list[index] = memory;
            }
            else
            {
                // NEW: Add to top
                list.Insert(0, memory);
            }

            await SaveToFile(list);
        }

        public static async Task DeleteMemoryAsync(string id)
        {
            var list = await LoadMemories();
            var item = list.FirstOrDefault(m => m.Id == id);
            if (item != null)
            {
                list.Remove(item);
                await SaveToFile(list);
            }
        }

        private static async Task SaveToFile(List<TravelMemory> list)
        {
            var json = JsonSerializer.Serialize(list);
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}