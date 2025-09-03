using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SteamAchievementCardManager.ViewModels
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool result = value != null;
            return invert ? !result : result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}