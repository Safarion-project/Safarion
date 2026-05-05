using Safarion.Models;
using Safarion.Services;
using System.Collections.ObjectModel;

namespace Safarion.Views
{
    public partial class TravelDiaryPage : ContentPage
    {
        public ObservableCollection<TravelMemory> Memories { get; set; } = new();

        public TravelDiaryPage()
        {
            InitializeComponent();
            MemoriesList.ItemsSource = Memories;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadData();
        }

        private async Task LoadData()
        {
            var data = await DiaryService.LoadMemories();
            Memories.Clear();
            foreach (var memory in data) Memories.Add(memory);
        }

        private async void OnAddMemoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddMemoryPage());
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var memory = button?.CommandParameter as TravelMemory;
            if (memory != null)
            {
                // Pass existing memory to edit page
                await Navigation.PushAsync(new AddMemoryPage(memory));
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            var memory = button?.CommandParameter as TravelMemory;
            if (memory != null)
            {
                bool answer = await DisplayAlert("Delete?", $"Remove '{memory.Title}'?", "Yes", "No");
                if (answer)
                {
                    await DiaryService.DeleteMemoryAsync(memory.Id);
                    await LoadData(); // Refresh list
                }
            }
        }
    }
}