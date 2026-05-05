using Safarion.Models;
using Safarion.Services;
using System.Collections.ObjectModel;

namespace Safarion.Views
{
    public partial class SafetyReviewsPage : ContentPage
    {
        public ObservableCollection<SafetyReview> Reviews { get; set; } = new();

        public SafetyReviewsPage()
        {
            InitializeComponent();
            ReviewsList.ItemsSource = Reviews;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadData();
        }

        private async Task LoadData()
        {
            var data = await ReviewService.GetReviews();

            data = data
                .OrderByDescending(x => x.Date)
                .ToList();

            Reviews.Clear();

            foreach (var item in data)
                Reviews.Add(item);
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddReviewPage());
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn &&
                btn.CommandParameter is SafetyReview review)
            {
                await Navigation.PushAsync(new AddReviewPage(review));
            }
        }

        // OPEN MAP PAGE
        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SafeMapPage());
        }

        // LIKE
        private async void OnLikeClicked(object sender, EventArgs e)
        {
            if (sender is Button btn &&
                btn.CommandParameter is SafetyReview review)
            {
                await ReviewService.LikeReview(review.Id);
                await LoadData();
            }
        }

        // DISLIKE
        private async void OnDislikeClicked(object sender, EventArgs e)
        {
            if (sender is Button btn &&
                btn.CommandParameter is SafetyReview review)
            {
                await ReviewService.DislikeReview(review.Id);
                await LoadData();
            }
        }
    }
}