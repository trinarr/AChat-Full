using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using AChatFull.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.IO;

namespace AChatFull.ViewModels
{
    public class EditProfileViewModel : INotifyPropertyChanged
    {
        readonly ChatRepository _repo;
        FileResult _pickedPhoto;

        public EditProfileViewModel()
        {
            _repo = new ChatRepository(App.DBPATH, App.USER_TOKEN_TEST);

            ChangeAvatarCommand = new Command(async () => await ChangeAvatarAsync());
            EnableBirthdateCommand = new Command(() => HasBirthdate = true);
            ClearBirthdateCommand = new Command(() => HasBirthdate = false);
            SaveCommand = new Command(async () => await SaveAsync());
            CloseCommand = new Command(async () => await Application.Current.MainPage.Navigation.PopModalAsync());

            _ = LoadAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region Bindable props

        // --- Busy state for async ops (loading/saving) ---
        bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                (SaveCommand as Command)?.ChangeCanExecute();
            }
        }

        ImageSource _avatarPreview;
        public ImageSource AvatarPreview
        {
            get => _avatarPreview;
            set
            {
                if (_avatarPreview == value) return;
                _avatarPreview = value;
                OnPropertyChanged(nameof(AvatarPreview));
            }
        }

        string _firstName;
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName == value) return;
                _firstName = value;
                OnPropertyChanged(nameof(FirstName));
            }
        }

        string _secondName;
        public string SecondName
        {
            get => _secondName;
            set
            {
                if (_secondName == value) return;
                _secondName = value;
                OnPropertyChanged(nameof(SecondName));
            }
        }

        string _about;
        public string About
        {
            get => _about;
            set
            {
                if (_about == value) return;
                _about = value;
                OnPropertyChanged(nameof(About));
            }
        }

        bool _hasBirthdate;
        public bool HasBirthdate
        {
            get => _hasBirthdate;
            set
            {
                if (_hasBirthdate == value) return;
                _hasBirthdate = value;
                OnPropertyChanged(nameof(HasBirthdate));
            }
        }

        DateTime _birthdateValue = new DateTime(DateTime.Now.Year - 20, 1, 1);
        public DateTime BirthdateValue
        {
            get => _birthdateValue;
            set
            {
                if (_birthdateValue == value) return;
                _birthdateValue = value;
                OnPropertyChanged(nameof(BirthdateValue));
            }
        }

        #endregion

        #region Commands
        public ICommand ChangeAvatarCommand { get; }
        public ICommand EnableBirthdateCommand { get; }
        public ICommand ClearBirthdateCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        async Task LoadAsync()
        {
            Debug.WriteLine("EditProfileViewModel LoadAsync ");

            IsBusy = true;
            try
            {
                var user = await _repo.GetCurrentUserProfileAsync();

                if(user.AvatarUrl != null) {
                    AvatarPreview = ImageSource.FromFile(user.AvatarUrl);
                    //AvatarPreview = ImageSource.FromUri(new Uri(user.AvatarUrl));
                }

                /*AvatarPreview = string.IsNullOrWhiteSpace(user?.AvatarUrl)
                    ? ImageSource.FromFile("avatar_placeholder.png")
                    : ImageSource.FromUri(new Uri(user.AvatarUrl));*/

                // Full name = FirstName + LastName

                FirstName = user?.FirstName?.Trim() ?? "";
                SecondName = user?.LastName?.Trim() ?? "";

                About = user?.About;

                if (user.BirthDate != null)
                {
                    HasBirthdate = true;
                    BirthdateValue = user.BirthDateDate;
                }
                else
                {
                    HasBirthdate = false;
                    //BirthdateValue = new DateTime(DateTime.Now.Year - 20, 1, 1);
                }
            }
            catch(Exception er)
            {
                Debug.WriteLine("EditProfileViewModel LoadAsync "+ er.Message+" "+ er.StackTrace);
            }
            finally
            {
                IsBusy = false;
            }
        }

        ImageSource BuildAvatarImage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();

            // data URI: data:image/png;base64,...
            if (s.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                var i = s.IndexOf(',');
                if (i > 0 && i < s.Length - 1)
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(s.Substring(i + 1));
                        return ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch { return null; }
                }
            }

            // Локальный файл-путь
            if (File.Exists(s))
                return ImageSource.FromFile(s);

            // Абсолютный URI (http/https/file/content и т.п.)
            if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                try
                {
                    // Для file:// можно сразу FromFile
                    if (uri.IsFile && !string.IsNullOrEmpty(uri.LocalPath) && File.Exists(uri.LocalPath))
                        return ImageSource.FromFile(uri.LocalPath);

                    // http/https/content:// — пробуем как Uri
                    return ImageSource.FromUri(uri);
                }
                catch { /* игнор, упадём в плейсхолдер */ }
            }

            // Если приходит относительный путь (например, "/avatars/a.png"),
            // можно склеить с базовым URL вашего API:
            // if (s.StartsWith("/")) {
            //     var baseUri = new Uri(App.ApiBaseUrl); // подставьте своё
            //     if (Uri.TryCreate(baseUri, s, out var abs))
            //         return ImageSource.FromUri(abs);
            // }

            return null;
        }

        async Task ChangeAvatarAsync()
        {
            try
            {
                var pick = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Choose a photo" });
                if (pick == null) return;

                _pickedPhoto = pick;

                using (var stream = await pick.OpenReadAsync())
                {
                    AvatarPreview = ImageSource.FromStream(() => stream);
                }
            }
            catch (FeatureNotSupportedException)
            {
                await Application.Current.MainPage.DisplayAlert("Unavailable", "Picking photos is not supported on this device.", "OK");
            }
            catch (PermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("Permission", "Allow photo permissions to change your avatar.", "OK");
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to pick the photo.", "OK");
            }
        }

        async Task SaveAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // 1) Сохраняем профиль
                var update = new ProfileUpdate
                {
                    FirstName = (FirstName ?? "").Trim(),
                    LastName = (SecondName ?? "").Trim(),
                    About = About,
                    Birthdate = HasBirthdate ? (DateTime?)BirthdateValue.Date : null
                };

                //await _repo.UpdateProfileAsync(update);

                // 2) Если выбран новый аватар — загружаем
                if (_pickedPhoto != null)
                {
                    using (var stream = await _pickedPhoto.OpenReadAsync())
                    {
                        //await _repo.UpdateAvatarAsync(stream, _pickedPhoto.FileName);
                    }
                }

                // 3) Сообщим другим экранам обновиться
                MessagingCenter.Send<object>(this, "ProfileChanged");

                //await Application.Current.MainPage.DisplayAlert("Saved", "Profile updated.", "OK");
                await Application.Current.MainPage.Navigation.PopModalAsync(); // ← модалка
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Could not save your changes.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    // DTO для обновления (подстрой к своей модели/репозиторию)
    public class ProfileUpdate
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string About { get; set; }
        public DateTime? Birthdate { get; set; }
    }
}