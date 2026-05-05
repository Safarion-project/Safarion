namespace Safarion
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // SWITCH BACK TO PROFESSIONAL MODE
            MainPage = new AppShell();
        }
    }
}