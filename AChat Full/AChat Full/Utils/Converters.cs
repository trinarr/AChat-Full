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
            => throw new NotSupportedException();
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

    // BoolToTextConverter: ConverterParameter="Открыть|Скачать"
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