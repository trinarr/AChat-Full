using System.Threading.Tasks;
using Xamarin.Forms;

namespace AChatFull.Views
{
    public interface ILazyInit
    {
        bool IsInitialized { get; }
        Task EnsureInitAsync(INavigation nav);
    }

    public interface ILazyInitPage
    {
        bool IsInitialized { get; }
        Task EnsureInitAsync(INavigation nav);
    }

    public interface ILazyInitViewModel
    {
        Task InitializeAsync(INavigation nav);
    }
}