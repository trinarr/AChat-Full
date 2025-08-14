using Xamarin.Forms;
using AChatFull.ViewModels;
using System;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    public partial class PinPage : ContentPage
    {
        public PinPage(bool isFirstRun, bool biometricsEnabled, Func<Task> onSuccess)
        {
            InitializeComponent();
            BindingContext = new PinViewModel(isFirstRun, biometricsEnabled, onSuccess);
        }

        protected override bool OnBackButtonPressed() => true;
    }
}