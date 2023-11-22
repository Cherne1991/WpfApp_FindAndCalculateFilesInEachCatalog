using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WpfApp_FindAndCalculateFilesInEachCatalog
{
    public static class SafeReadDirectory
    {
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*.*", SearchOption searchOpt = SearchOption.AllDirectories)
        {
            try
            {
                var dirFiles = Enumerable.Empty<string>();

                if (searchOpt == SearchOption.AllDirectories)
                {
                    dirFiles = Directory.EnumerateDirectories(path)
                                        .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
                }

                return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}
