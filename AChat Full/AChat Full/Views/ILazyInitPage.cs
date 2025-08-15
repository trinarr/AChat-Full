using System.Threading.Tasks;
using Xamarin.Forms;

namespace AChatFull.Views
{
    public interface ILazyInitPage
    {
        bool IsInitialized { get; }
        Task EnsureInitAsync(INavigation nav);
    }
}