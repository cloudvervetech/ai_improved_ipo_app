using System.Globalization;

namespace AI_IMPROVED_IPO_APP.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                var parts = paramString.Split('|');
                if (parts.Length == 2)
                {
                    var colorName = boolValue ? parts[0] : parts[1];
                    return Color.FromArgb(GetColorHex(colorName));
                }
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetColorHex(string colorName)
        {
            return colorName.ToLower() switch
            {
                "green" => "#00C853",
                "red" => "#D32F2F",
                "blue" => "#1976D2",
                "yellow" => "#FDD835",
                "orange" => "#FF6F00",
                _ => "#9E9E9E"
            };
        }
    }
}
