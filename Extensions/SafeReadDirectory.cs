using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WpfApp_FindAndCalculateFilesInEachCatalog
{
    public static class SafeReadDirectory
    {
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt, bool touchAllFiles = false)
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

        public static List<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }
    }
}
