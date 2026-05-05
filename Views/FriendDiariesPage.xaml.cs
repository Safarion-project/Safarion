using Safarion.Models;
using Safarion.Services;
using System.Collections.ObjectModel;

namespace Safarion.Views
{
    public partial class FriendDiariesPage : ContentPage
    {
        public ObservableCollection<TravelMemory> PublicMemories { get; set; } = new();
        private string _friendName;

        public FriendDiariesPage(string friendName)
        {
            InitializeComponent();
            _friendName = friendName;

            FriendNameLabel.Text = friendName;
            FriendInitial.Text = string.IsNullOrEmpty(friendName) ? "F" : friendName[0].ToString().ToUpper();

            MemoriesList.ItemsSource = PublicMemories;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPublicMemories();
        }

        private async Task LoadPublicMemories()
        {
            try
            {
                // Load ALL memories
                var allMemories = await DiaryService.LoadMemories();

                // Filter to only public ones created by this friend
                var publicMemories = allMemories
                    .Where(m => m.IsPublic && m.CreatedBy == _friendName)
                    .OrderByDescending(m => m.StartDate)
                    .ToList();

                PublicMemories.Clear();
                foreach (var memory in publicMemories)
                {
                    PublicMemories.Add(memory);
                }

                // Update count label
                if (publicMemories.Count == 0)
                {
                    MemoryCountLabel.Text = "No public stories shared";
                }
                else if (publicMemories.Count == 1)
                {
                    MemoryCountLabel.Text = "1 public story";
                }
                else
                {
                    MemoryCountLabel.Text = $"{publicMemories.Count} public stories";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FriendDiaries] Error: {ex.Message}");
                MemoryCountLabel.Text = "Could not load stories";
            }
        }
    }
}