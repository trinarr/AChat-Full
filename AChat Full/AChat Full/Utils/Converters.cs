using System;
using System.Globalization;
using AChatFull.Views;
using Xamarin.Forms;

namespace AChatFull.Utils
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }
    }

    public class ClientToImageConverter : IValueConverter
    {
        public string ICQImage { get; set; } = "ICQ.png";
        public string ICQ2Image { get; set; } = "ICQ2.png";
        public string AIMImage { get; set; } = "AIM.png";
        public string QIPImage { get; set; } = "QIP.png";
        public string RQImage { get; set; } = "RQ.png";
        public string MIRCImage { get; set; } = "MIRC.png";
        public string MIRANDAImage { get; set; } = "MIRANDA.png";
        public string JIMMImage { get; set; } = "JIMM.png";
        public string INFIUMImage { get; set; } = "INFIUM.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ClientType p)
            {
                switch (p)
                {
                    case ClientType.ICQ2: return ICQ2Image;
                    case ClientType.ICQ: return ICQImage;
                    case ClientType.AIM: return AIMImage;
                    case ClientType.QIP: return QIPImage;
                    case ClientType.RQ: return RQImage;
                    case ClientType.MIRC: return MIRCImage;
                    case ClientType.MIRANDA: return MIRANDAImage;
                    case ClientType.JIMM: return JIMMImage;
                    case ClientType.INFIUM: return INFIUMImage;
                }
            }
            if (value is string s && Enum.TryParse<Presence>(s, true, out var pres))
                return Convert(pres, targetType, parameter, culture);

            return ICQImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class PresenceToImageConverter : IValueConverter
    {
        // Имена файлов (лежать в Resources/Icons/*). Можно переопределить в XAML.
        public string OnlineImage { get; set; } = "icon_online.png";
        public string IdleImage { get; set; } = "icon_away.png";
        public string DoNotDisturbImage { get; set; } = "icon_dnd.png";
        public string OfflineImage { get; set; } = "icon_offline.png";
        public string InvisibleImage { get; set; } = "icon_invisible.png";
        public string FallbackImage { get; set; } = "status.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Presence p)
            {
                switch (p)
                {
                    case Presence.Online: return OnlineImage;
                    case Presence.Idle: return IdleImage;
                    case Presence.DoNotDisturb: return DoNotDisturbImage;
                    case Presence.Invisible: return OfflineImage;
                    case Presence.Offline: return OfflineImage;
                }
            }
            if (value is string s && Enum.TryParse<Presence>(s, true, out var pres))
                return Convert(pres, targetType, parameter, culture);

            return FallbackImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// Возвращает true, если value == parameter (без учёта регистра).
    /// В ConvertBack при установке true возвращает параметр (для TwoWay биндинга).
    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is bool b && b) ? parameter?.ToString() : Binding.DoNothing;
    }

    public class PresenceToTextConverter : IValueConverter
    {
        // По умолчанию — «DoNotDisturb». Поставь false, если хочешь «Do not disturb».
        public bool Compact { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Presence p)
                return Compact ? p.ToLabel() : p.ToReadableLabel();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class PresenceToColorConverter : IValueConverter
    {
        public Color Online { get; set; } = Color.FromHex("#23A55A");     // зелёный
        public Color Idle { get; set; } = Color.FromHex("#F0B232");       // жёлтый
        public Color DoNotDisturb { get; set; } = Color.FromHex("#F23F43"); // красный
        public Color Offline { get; set; } = Color.FromHex("#80848E");    // серый

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Presence p)
            {
                switch (p)
                {
                    case Presence.Online: return Online;
                    case Presence.Idle: return Idle;
                    case Presence.DoNotDisturb: return DoNotDisturb;
                    default: return Offline;
                }
            }
            return Offline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ничего не меняем в исходном свойстве, даже если нас случайно вызвали
            return Binding.DoNothing;
        }
    }

    public class BoolToGridColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? 1 : 2;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FirstLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? string.Empty : str.Substring(0, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converters/EqualConverter.cs
    public class EqualConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value?.ToString()?.Equals(p?.ToString(), StringComparison.OrdinalIgnoreCase) == true;
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    // Converters/FileSizeConverter.cs
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            var hasUrl = false;
            if (p as string == "fromMsg" && v is ChatMessage m && m.Document != null)
                hasUrl = !string.IsNullOrEmpty(m.Document.RemoteUrl);

            if (v is long b)
            {
                string[] units = { "Б", "КБ", "МБ", "ГБ" };
                double s = b; int i = 0;
                while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
                return $"{s:0.#} {units[i]}";
            }

            return "Размер неизвестен";
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            var parts = (parameter?.ToString() ?? "Да|Нет").Split('|');
            return (value is bool b && b) ? parts[0] : (parts.Length > 1 ? parts[1] : parts[0]);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

    public class BoolToHorzOptionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var incoming = value is bool b && b;
            // true = входящее (слева), false = исходящее (справа)
            return incoming ? LayoutOptions.Start : LayoutOptions.End;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class IncomingBgConverter : IValueConverter
    {
        // Можно переопределить через XAML сеттерами, если захотите другие цвета
        public Color IncomingColor { get; set; } = Color.FromHex("#f5f5f5"); // серый
        public Color OutgoingColor { get; set; } = Color.FromHex("#0084FF"); // синий

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var incoming = value is bool b && b;
            return incoming ? IncomingColor : OutgoingColor;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class IncomingTextColorConverter : IValueConverter
    {
        public Color IncomingText { get; set; } = Color.Black;
        public Color OutgoingText { get; set; } = Color.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var incoming = value is bool b && b;
            return incoming ? IncomingText : OutgoingText;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}