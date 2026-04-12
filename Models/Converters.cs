using System.Globalization;

namespace Collection_Management.Models
{
    public class ItemStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status switch
                {
                    ItemStatus.Possess => Color.FromArgb("#8964E3"),
                    ItemStatus.WantToSell => Color.FromArgb("#6490E3"),
                    ItemStatus.Sold => Color.FromArgb("#6468E3"),
                    _ => Color.FromArgb("#64B8E3")
                };
            }
            return Color.FromArgb("#64B8E3");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ItemStatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status switch
                {
                    ItemStatus.Possess => Color.FromArgb("#8964E3"),      // Light gray for Possess
                    ItemStatus.WantToSell => Color.FromArgb("#6490E3"),   // Light yellow for WantToSell
                    ItemStatus.Sold => Color.FromArgb("#B364E3"),         // Light purple for Sold
                    _ => Colors.White
                };
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusString && Enum.TryParse<ItemStatus>(statusString, out var result))
            {
                return result;
            }
            return ItemStatus.Possess;
        }
    }
}
