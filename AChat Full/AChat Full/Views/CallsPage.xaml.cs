using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CallsPage : ContentPage
    {
        // ВАЖНО: как у ContactsPage — принимаем репозиторий в конструктор
        public CallsPage(ChatRepository chatRepository)
        {
            InitializeComponent();
            BindingContext = new CallsViewModel(chatRepository);
        }

        // опционально: второй конструктор на случай XAML-превью/дизайнера
        public CallsPage() : this(DependencyService.Get<ChatRepository>()) { }
    }
}