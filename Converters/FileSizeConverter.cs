using System;
using System.Globalization;
using System.Windows.Data;
using WpfApp_FindAndCalculateFilesInEachCatalog.Models;

namespace WpfApp_FindAndCalculateFilesInEachCatalog.Converters
{
    internal class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long fileSize = (long)value;

            return fileSize.ToSize(SizeUnits.MB);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
