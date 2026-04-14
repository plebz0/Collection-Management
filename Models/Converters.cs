using System.Globalization;


namespace Collection_Management.Models
{
    // Converts an ItemStatus enum value to a Color for border styling
    // Used to visually distinguish item status through different border colors
    public class ItemStatusColorConverter : IValueConverter
    {

        // Converts ItemStatus enum to a Color value for UI display
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status switch
                {
                    ItemStatus.Possess => Color.FromArgb("#371CFC"),       
                    ItemStatus.WantToSell => Color.FromArgb("#1CD6FC"),   
                    ItemStatus.Sold => Color.FromArgb("#9034FF"),          
                    _ => Color.FromArgb("#42A3FF")                         
                };
            }
            return Color.FromArgb("#42A3FF");
        }

  
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

  
    // Converts an ItemStatus enum value to a Color for item card background styling

    public class ItemStatusBackgroundConverter : IValueConverter
    {
        // Converts ItemStatus enum to a background Color value for UI display
 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status switch
                {
                    ItemStatus.Possess => Color.FromArgb("#F0F4FF"),      
                    ItemStatus.WantToSell => Color.FromArgb("#F0F9FF"),    
                    ItemStatus.Sold => Color.FromArgb("#F9F5FF"),         
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

    // Konwertuje między ItemStatus enum a stringiem - dla Picker bindowania
    public class StringToEnumConverter : IValueConverter
    {
        // Konwertuje enum na string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status.ToString();
            }
            return string.Empty;
        }

        // Konwertuje string z powrotem na enum
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusString && Enum.TryParse<ItemStatus>(statusString, out var result))
            {
                return result;  
            }
            return ItemStatus.Possess;  
        }
    }

    // Converts Base64 string to ImageSource - loads images from database
    public class Base64ToImageSourceConverter : IValueConverter
    {
        // Decodes Base64 to ImageSource
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64String && !string.IsNullOrEmpty(base64String))
            {
                try
                {
                    byte[] imageBytes = System.Convert.FromBase64String(base64String);
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        // Nie implementuje zwrotnego konwertowania
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    // Converts ItemStatus to Polish text for UI display
    public class ItemStatusToPolishConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemStatus status)
            {
                return status switch
                {
                    ItemStatus.Possess => "W posiadaniu",
                    ItemStatus.WantToSell => "Chce Sprzedać",
                    ItemStatus.Sold => "Sprzedane",
                    _ => "Nieznany"
                };
            }
            return "Nieznany";
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "W posiadaniu" => ItemStatus.Possess,
                    "Chce Sprzedać" => ItemStatus.WantToSell,
                    "Sprzedane" => ItemStatus.Sold,
                    _ => ItemStatus.Possess
                };
            }
            return ItemStatus.Possess;
        }
    }

}


