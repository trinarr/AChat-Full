using System.Linq;
using Xamarin.Forms;
using System.Diagnostics;
using System;
using System.ComponentModel;

namespace AChatFull.Views
{
    public partial class PhonePage : ContentPage, INotifyPropertyChanged
    {
        private int phoneLength;
        /// <summary>
        /// Активно, когда введено >=10 цифр.
        /// </summary>
        public bool CanSendCode => phoneLength >= 11;

        public PhonePage()
        {
            InitializeComponent();
            BindingContext = this;

            //SendCodeCommand = new Command(OnSendCode);

            // чтобы обновлять CanSendCode при каждом вводе
            PhoneEntry.TextChanged += (s, e) =>
            {
                var digits = new string(PhoneEntry.Text.Where(char.IsDigit).ToArray());
                phoneLength = digits.Count(char.IsDigit);

                // PhoneNumber has already been set via two‐way binding, so just:
                base.OnPropertyChanged(nameof(CanSendCode));
            };
        }

        private async void OnSendCodeClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("TESTLOG OnSendCode " + PhoneEntry.Text);

            var digits = new string(PhoneEntry.Text.Where(char.IsDigit).ToArray());

            // TODO: отправка SMS на номер `full`
            // await AuthService.SendSmsCodeAsync(full);

            // Переход на ввод кода
            await Navigation.PushAsync(new SmsCodePage("+" + digits));
        }
    }
}