using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Essentials;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace AChatFull.ViewModels
{
    public enum PinMode { Create, Confirm, Verify }

    public class PinViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private const string PinKey = "user_pin";
        private const string BioKey = "bio_enabled"; // "1" / "0"

        private readonly Func<Task> _onSuccess;
        private string _entered = string.Empty;
        private string _firstPin = null;
        private PinMode _mode;
        private string _titleText;
        private bool _biometricsEnabled;
        private bool _bioBusy;

        public PinMode Mode
        {
            get => _mode;
            private set { if (_mode != value) { _mode = value; OnPropertyChanged(); } }
        }

        public string TitleText
        {
            get => _titleText;
            private set { if (_titleText != value) { _titleText = value; OnPropertyChanged(); } }
        }

        private bool _showBiometricButton;
        public bool ShowBiometricButton
        {
            get => _showBiometricButton;
            private set
            {
                if (_showBiometricButton != value)
                {
                    _showBiometricButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool BiometricsEnabled
        {
            get => _biometricsEnabled;
            private set { if (_biometricsEnabled != value) { _biometricsEnabled = value; OnPropertyChanged(); } }
        }

        public bool Dot1 => _entered.Length > 0;
        public bool Dot2 => _entered.Length > 1;
        public bool Dot3 => _entered.Length > 2;
        public bool Dot4 => _entered.Length > 3;

        public ICommand NumberCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand BiometricsCommand { get; }

        public PinViewModel(bool isFirstRun, bool biometricsEnabled, Func<Task> onSuccess)
        {
            _onSuccess = onSuccess ?? (() => Task.CompletedTask);

            Mode = isFirstRun ? PinMode.Create : PinMode.Verify;
            TitleText = isFirstRun ? "Придумайте PIN‑код" : "Введите PIN‑код";
            BiometricsEnabled = biometricsEnabled;
            ShowBiometricButton = biometricsEnabled; // показывать кнопку только если разрешено

            NumberCommand = new Command<string>(OnNumber);
            DeleteCommand = new Command(OnDelete);
            // Кнопка слева внизу — всегда форсирует показ диалога
            BiometricsCommand = new Command(async () => await TryBiometricsAsync(force: true));

            // Автозапуск при обычном входе (если включено в настройках)
            if (!isFirstRun && biometricsEnabled)
                Device.BeginInvokeOnMainThread(async () => await TryBiometricsAsync(force: false));
        }

        private void OnNumber(string num)
        {
            if (string.IsNullOrEmpty(num)) return;
            if (_entered.Length >= 4) return;

            _entered += num;
            RaiseDots();

            if (_entered.Length == 4)
                _ = ProcessPinAsync();
        }

        private void OnDelete()
        {
            if (_entered.Length == 0) return;
            _entered = _entered.Substring(0, _entered.Length - 1);
            RaiseDots();
        }

        private void RaiseDots()
        {
            OnPropertyChanged(nameof(Dot1));
            OnPropertyChanged(nameof(Dot2));
            OnPropertyChanged(nameof(Dot3));
            OnPropertyChanged(nameof(Dot4));
        }

        private async Task ProcessPinAsync()
        {
            switch (Mode)
            {
                case PinMode.Create:
                    _firstPin = _entered;
                    _entered = string.Empty;
                    TitleText = "Повторите PIN‑код";
                    Mode = PinMode.Confirm;
                    RaiseDots();
                    break;

                case PinMode.Confirm:
                    if (_entered == _firstPin)
                    {
                        await SecureStorage.SetAsync(PinKey, _entered);
                        _entered = string.Empty;
                        RaiseDots();

                        await AskBiometricsPermissionAsync();
                        await _onSuccess();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "PIN‑коды не совпадают. Придумайте новый.", "OK");
                        ResetToCreate();
                    }
                    break;

                case PinMode.Verify:
                    var saved = await SecureStorage.GetAsync(PinKey);
                    if (!string.IsNullOrEmpty(saved) && _entered == saved)
                    {
                        _entered = string.Empty;
                        RaiseDots();
                        await _onSuccess();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Неверный PIN", "Попробуйте ещё раз.", "OK");
                        _entered = string.Empty;
                        RaiseDots();
                    }
                    break;
            }
        }

        private void ResetToCreate()
        {
            _firstPin = null;
            _entered = string.Empty;
            Mode = PinMode.Create;
            TitleText = "Придумайте PIN‑код";
            RaiseDots();
        }

        private async Task TryBiometricsAsync(bool force)
        {
            if (_bioBusy) return;
            _bioBusy = true;
            try
            {
                // есть ли вообще биометрия на устройстве
                var available = await CrossFingerprint.Current.IsAvailableAsync(true);
                if (!available)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Биометрия недоступна",
                        "На устройстве не настроен отпечаток/FaceID.",
                        "OK");
                    return;
                }

                // если вызов НЕ форсируемый и пользователь её отключил — выходим
                if (!force && !BiometricsEnabled) return;

                var cfg = new AuthenticationRequestConfiguration("Вход", "Подтвердите по отпечатку")
                {
                    // опционально:
                    CancelTitle = "Отмена",
                    FallbackTitle = "Использовать PIN"
                };

                var result = await CrossFingerprint.Current.AuthenticateAsync(cfg);
                if (result.Authenticated)
                {
                    _entered = string.Empty;
                    RaiseDots();
                    await _onSuccess();
                }
                // если неуспех — просто остаёмся на экране PIN
            }
            finally { _bioBusy = false; }
        }

        private async Task AskBiometricsPermissionAsync()
        {
            var available = await CrossFingerprint.Current.IsAvailableAsync(true);
            if (!available)
            {
                BiometricsEnabled = false;
                await SecureStorage.SetAsync(BioKey, "0");
                return;
            }

            var allow = await Application.Current.MainPage.DisplayAlert(
                "Биометрия", "Включить вход по отпечатку пальца?", "Да", "Нет");

            BiometricsEnabled = allow;
            ShowBiometricButton = allow;
            await SecureStorage.SetAsync(BioKey, allow ? "1" : "0");
        }
    }
}