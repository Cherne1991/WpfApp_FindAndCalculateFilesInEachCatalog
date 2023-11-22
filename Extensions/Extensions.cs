using System;
using System.IO;
using WpfApp_FindAndCalculateFilesInEachCatalog.Models;

namespace WpfApp_FindAndCalculateFilesInEachCatalog
{
    public static class Extensions
    {
        public static long GetFileSize(string file)
        {
            return new FileInfo(file).Length;
        }

        public static string ToSize(this long value, SizeUnits unit)
        {
            return (value / (double)Math.Pow(1024, (long)unit)).ToString(SizeUnits.Byte == unit ? "0" : "0.00");
        }
    }
}
