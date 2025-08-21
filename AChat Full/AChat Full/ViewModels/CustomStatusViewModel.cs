using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.ComponentModel;
using AChatFull.Views;
using System.Threading.Tasks;

namespace AChatFull.ViewModels
{
    public class CustomStatusViewModel : INotifyPropertyChanged
    {
        private readonly ChatRepository _repo;
        private string _statusEmoji;
        private string _statusText;

        public string StatusEmoji
        {
            get => _statusEmoji;
            set { if (_statusEmoji != value) { _statusEmoji = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEmoji)); } }
        }
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
        }
        public bool HasEmoji => !string.IsNullOrWhiteSpace(StatusEmoji);

        public ObservableCollection<string> EmojiChoices { get; } = new ObservableCollection<string>();
        public ICommand PickEmojiCommand { get; }

        public CustomStatusViewModel(ChatRepository repo)
        {
            _repo = repo;
            PickEmojiCommand = new Command<string>(emoji =>
            {
                if (!string.IsNullOrWhiteSpace(emoji))
                    StatusEmoji = emoji;
            });
            LoadEmojiChoices();
        }

        public async Task ReloadStatusAsync()
        {
            var s = await _repo.GetCustomStatusAsync(); // ваш метод чтения
            StatusEmoji = s?.Emoji;
            StatusText = s?.Text;
        }

        private void LoadEmojiChoices()
        {
            var list = new[]
            {
            "😀","😃","😄","😁","😆","😅","😂","🤣",
            "😊","🙂","🙃","😉","😌","😍","🥰","😘",
            "😗","😙","😚","😋","😜","🤪","😝","🫠",
            "🤗","🤭","🤫","🤔","🫡","🤐","😐","😑",
            "😶","🫥","😏","😒","🙄","😬","🤥","😴",
            "🤒","🤕","🤢","🤮","🤧","😷","🥵","🥶",
            "😎","🤓","🧐","😕","☹️","🙁","😟","😢",
            "😭","😤","😠","😡","🤬","🤯","😳","😱",
        };
            foreach (var e in list) EmojiChoices.Add(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

}