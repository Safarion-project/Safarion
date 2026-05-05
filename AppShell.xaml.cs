namespace Safarion
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register the route so "//ProfilePage" works
            Routing.RegisterRoute("ProfilePage", typeof(Views.ProfilePage));

            // Register new routes for navigation
            Routing.RegisterRoute("FriendDiariesPage", typeof(Views.FriendDiariesPage));
        }
    }
}