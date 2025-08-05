using System;
using System.Globalization;
using Xamarin.Forms;

namespace AChatFull
{
    public class InverseBoolConverter : IValueConverter
    {
        // value — входящее значение (object), parameter и culture обычно не используются
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true; // или false, в зависимости от логики
        }

        // Обратное преобразование, если нужно (обычно не применяется)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }
    }
}