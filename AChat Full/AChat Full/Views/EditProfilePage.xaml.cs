using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditProfilePage : ContentPage
    {
        public EditProfilePage()
        {
            InitializeComponent();
            BindingContext = new EditProfileViewModel();
        }
    }
}
