using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading;
using System.Threading.Tasks;

namespace AChatFull.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SmsCodePage : ContentPage
    {
        private readonly string _phone;
        private int _secondsLeft = 60;
        private Timer _timer;
        private int codeLength;

        public string Code { get; set; }
        public bool CanVerify => codeLength >= 4;
        public bool CanResend => _secondsLeft <= 0;
        public string CountdownText => _secondsLeft > 0
            ? $"Повторно через {_secondsLeft} сек."
            : "Можете отправить код заново";

        public SmsCodePage(string phoneNumber)
        {
            InitializeComponent();
            _phone = phoneNumber;
            BindingContext = this;

            InfoLabel.Text = $"Код отправлен на {_phone}";

            CodeEntry.TextChanged += (s, e) =>
            {
                codeLength = CodeEntry.Text.Length;
                OnPropertyChanged(nameof(CanVerify));
            };

            StartCountdown();
        }

        private void StartCountdown()
        {
            _secondsLeft = 60;
            _timer?.Dispose();
            _timer = new Timer(_ =>
            {
                _secondsLeft--;
                Device.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(CountdownText));
                    OnPropertyChanged(nameof(CanResend));
                });
                if (_secondsLeft <= 0)
                    _timer.Dispose();
            }, null, 0, 1000);
        }

        private async void OnVerifyCodeClicked(object sender, EventArgs e)
        {
            Code = CodeEntry.Text.Trim();
            // TODO: проверить код на сервере:
            // bool ok = await AuthService.VerifySmsCodeAsync(_phone, Code);
            bool ok = true; // заглушка

            if (ok)
            {
                // Успешная авторизация — переходим дальше
                await ChatInitAsync();
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный код", "ОК");
            }
        }

        private async Task ChatInitAsync()
        {
            var dbPath = await PreloadDatabase.GetDatabasePathAsync();

            // например, сохраняем в DependencyService или сразу передаём в ViewModel:
            var repo = new ChatRepository(dbPath, App.USER_TOKEN_TEST);
            Application.Current.MainPage = new NavigationPage(new ChatsListPage(App.USER_TOKEN_TEST, repo));
        }

        private async void OnResendClicked(object sender, EventArgs e)
        {
            // TODO: повторно отправить SMS
            // await AuthService.SendSmsCodeAsync(_phone);
            StartCountdown();
        }
    }
}