using System.Threading.Tasks;

namespace AChatFull.Services
{
    public interface IIconSwitchService
    {
        Task SwitchAppIcon(int iconType);
    }
}
