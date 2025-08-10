using System;
using System.Globalization;
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
            if (v is long b)
            {
                string[] units = { "Б", "КБ", "МБ", "ГБ" };
                double s = b; int i = 0;
                while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
                return $"{s:0.#} {units[i]}";
            }
            return "";
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
}