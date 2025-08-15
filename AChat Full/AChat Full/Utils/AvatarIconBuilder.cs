using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AChatFull.Utils
{
    /// <summary>
    /// Контракт для генерации PNG-иконки: круглая аватарка + точка статуса.
    /// presence: "Online" / "Away" / "Busy" / "Offline" (регистр не важен).
    /// sizeDp: целевой размер иконки в dp (обычно 24–32 dp).
    /// </summary>
    public interface IAvatarIconBuilder
    {
        Task<ImageSource> BuildAsync(
            string avatarUrlOrPath,
            string initials,
            string presence,
            int sizeDp,
            int? statusSizeDp = null);
    }

    /// <summary>
    /// Фасад через DependencyService.
    /// </summary>
    public static class AvatarIconBuilder
    {
        public static Task<ImageSource> BuildAsync(
            string avatarUrlOrPath,
            string initials,
            string presence,
            int sizeDp,
            int? statusSizeDp = null)
        {
            var impl = DependencyService.Get<IAvatarIconBuilder>();
            if (impl == null)
                return Task.FromResult<ImageSource>(null);

            return impl.BuildAsync(avatarUrlOrPath, initials, presence, sizeDp, statusSizeDp);
        }

        /// <summary>
        /// Простейшее получение инициалов из "Имя Фамилия".
        /// </summary>
        public static string MakeInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(1, parts[0].Length)).ToUpperInvariant();

            var first = parts[0];
            var last = parts[parts.Length - 1];
            var a = first.Length > 0 ? first[0].ToString() : "";
            var b = last.Length > 0 ? last[0].ToString() : "";
            var ab = (a + b);
            return string.IsNullOrEmpty(ab) ? "?" : ab.ToUpperInvariant();
        }
    }
}
