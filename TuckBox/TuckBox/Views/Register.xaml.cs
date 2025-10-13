using TuckBox.ViewModels;

namespace TuckBox.Views;

public partial class Register : ContentPage
{
    public Register(RegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
