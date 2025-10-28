namespace TuckBox
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Routes 
            Routing.RegisterRoute("Main", typeof(TuckBox.MainPage));
            Routing.RegisterRoute("Register", typeof(TuckBox.Views.Register));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.Login));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.CurrentOrder));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.PlaceOrder));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.OrderHistory));
            Routing.RegisterRoute("Login", typeof(TuckBox.Views.UpdateUser));
        }
    }
}
