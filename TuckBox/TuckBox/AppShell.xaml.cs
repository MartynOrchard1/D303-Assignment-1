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
            Routing.RegisterRoute("CurrentOrder", typeof(TuckBox.Views.CurrentOrder));
            Routing.RegisterRoute("PlaceOrder", typeof(TuckBox.Views.PlaceOrder));
            Routing.RegisterRoute("OrderHistory", typeof(TuckBox.Views.OrderHistory));
            Routing.RegisterRoute("UpdateUser", typeof(TuckBox.Views.UpdateUser));
        }
    }
}
