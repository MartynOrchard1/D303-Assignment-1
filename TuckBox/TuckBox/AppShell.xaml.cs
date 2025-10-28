namespace TuckBox
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Routes 
            Routing.RegisterRoute("Register", typeof(TuckBox.Views.Register));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.Login));
            Routing.RegisterRoute("Main", typeof(TuckBox.MainPage));
        }
    }
}
