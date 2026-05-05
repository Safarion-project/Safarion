namespace Safarion.Views;

public partial class ProfilePhotoViewPage : ContentPage
{
    public ProfilePhotoViewPage(string imagePath)
    {
        InitializeComponent();

        FullImage.Source = ImageSource.FromFile(imagePath);
    }
}