namespace Safarion.Views
{
    public partial class HowToUsePage : ContentPage
    {
        public HowToUsePage()
        {
            InitializeComponent();
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            // If it was opened as a popup (first launch), pop modal. 
            // If opened from a navigation menu, pop standard.
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
            else
                await Navigation.PopAsync();
        }
    }
}