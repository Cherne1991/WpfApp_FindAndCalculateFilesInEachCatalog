using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

namespace WpfApp_FindAndCalculateFilesInEachCatalog.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private const string SearchText = "Search";
        private const string ResetText = "Reset";
        private const string PauseText = "Pause";
        private const string ResumeText = "Resume";
        private const string EmptyText = "";
        private const int File10MB = 10 * 1024 * 1024;
        private ManualResetEvent _pauseEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;

        public MainViewModel()
        {
            InitializeDrives();

            SearchOrResetText = SearchText;
            PauseResumeText = EmptyText;

            //CatalogList.CollectionChanged += CatalogList_CollectionChanged;
        }

        //private async void CatalogList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    TotalFileSize = 0;
        //    TotalFileCount = 0;

        //    _cancellationTokenSource = new CancellationTokenSource();
        //    _pauseEvent = new ManualResetEvent(true);

        //    string[] subDirs = { };
        //    string[] files = { };

        //    //try
        //    //{
        //    //    subDirs = Directory.GetDirectories(DriveSelectedItem);

        //    //    await Task.Run(CalculatingInFileCountAndSizeEachCatalog, _cancellationTokenSource.Token);
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    Debug.WriteLine(e.Message);
        //    //}
        //}

        private long _totalFileCount;

        public long TotalFileCount
        {
            get => _totalFileCount;
            set
            {
                _totalFileCount = value;
                OnPropertyChanged(nameof(TotalFileCount));
            }
        }

        private long _totalSizesFile;

        public long TotalFileSize
        {
            get => _totalSizesFile;
            set
            {
                _totalSizesFile = value;
                OnPropertyChanged(nameof(TotalFileSize));
            }
        }

        private bool _isRunSearch;

        public bool IsRunSearch
        {
            get => _isRunSearch;
            set
            {
                _isRunSearch = value;
                OnPropertyChanged(nameof(IsRunSearch));
            }
        }

        private string _pauseResumeText;

        public string PauseResumeText
        {
            get => _pauseResumeText;
            set
            {
                _pauseResumeText = value;
                OnPropertyChanged(nameof(PauseResumeText));
            }
        }

        private string _searchOrResetText;

        public string SearchOrResetText
        {
            get => _searchOrResetText;
            set
            {
                _searchOrResetText = value;
                OnPropertyChanged(nameof(SearchOrResetText));
            }
        }

        public ObservableCollection<RowViewModel> CatalogList { get; set; } = new ObservableCollection<RowViewModel>();

        public ObservableCollection<string> Drives { get; set; } = new ObservableCollection<string>();

        private string _driveSelectedItem;

        public string DriveSelectedItem
        {
            get => _driveSelectedItem;
            set
            {
                _driveSelectedItem = value;
                OnPropertyChanged(nameof(DriveSelectedItem));
            }
        }

        private void InitializeDrives()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                Drives.Add(drive.Name);
            }

            if (Drives.Count > 0)
            {
                DriveSelectedItem = Drives.First();
            }
        }

        //public async Task<IEnumerable<string>> EnumerateFiles(string path, string searchPattern, CancellationTokenSource cancellationTokenSource)
        //{
        //    try
        //    {
        //        var dirFiles = Directory.EnumerateDirectories(path);

        //        //var files = Directory.GetFiles();
        //        { }

        //        //await Task.Run(async () =>
        //        //{
        //            foreach (var dir in dirFiles)
        //            {
        //                var allFiles = Directory.GetFiles(dir);
        //                var hasFilesMore10MB = allFiles.Any(f => Extensions.GetFileSize(f) > File10MB);

        //                if (hasFilesMore10MB)
        //                {
        //                    var rowViewModel = new RowViewModel(new DirectoryInfo(dir).Name, dir, _pauseEvent, _cancellationTokenSource?.Token ?? default);

        //                    //rowViewModel.Calculate();

        //                    App.Current?.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel));
        //                }

        //                EnumerateFiles(dir, searchPattern, cancellationTokenSource);
        //            }
        //        //}, cancellationTokenSource.Token);



        //        //if (searchOpt == SearchOption.AllDirectories)
        //        //{
        //        //    dirFiles = Directory.EnumerateDirectories(path)
        //        //                        .SelectMany(async x => await Task.Run(() => EnumerateFiles(x, searchPattern, searchOpt)));

        //        //    //foreach (var catalog in dirFiles)
        //        //    //{
        //        //    //    var rowViewModel = new RowViewModel(new DirectoryInfo(catalog).Name, catalog, _pauseEvent, _cancellationTokenSource?.Token ?? default);

        //        //    //    CatalogList.Add(rowViewModel);
        //        //    //}
        //        //}

        //        //var hhhh = dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern)).Where(f => Extensions.GetFileSize(f) > File10MB);

        //        //foreach (var catalog in hhhh)
        //        //{
        //        //    var rowViewModel = new RowViewModel(new DirectoryInfo(catalog).Name, catalog, _pauseEvent, _cancellationTokenSource?.Token ?? default);

        //        //    App.Current.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel));
        //        //}

        //        return dirFiles;
        //    }
        //    catch (Exception)
        //    {
        //        return Enumerable.Empty<string>();
        //    }
        //}

        //public Task<IEnumerable<string>> EnumerateFiles(string path, string searchPattern)
        //{
        //    try
        //    {
        //        var rowViewModel = new RowViewModel(new DirectoryInfo(path).Name, path, new ManualResetEvent(true), default);

        //        App.Current?.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel));


        //        var dirFiles = Directory.EnumerateDirectories(path)
        //                                .SelectMany(x => EnumerateFiles(x, searchPattern).Result);
        //        var yyy = dirFiles.Count();
        //        { }



        //        return Task.FromResult(dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern)));
        //    }
        //    catch (Exception)
        //    {
        //        return Task.FromResult(Enumerable.Empty<string>());
        //    }
        //}

        //private async void GetRecursFiles(string start_path)
        //{
        //    var ls = new List<string>();
        //    try
        //    {
        //        //await Task.Run(async () =>
        //        //{
        //        //var hhhh = EnumerateFiles(start_path, "*.*", _cancellationTokenSource);

        //        var ffghhj = await EnumerateFiles(start_path, "*.*");
        //        var jjj = ffghhj.Count();
        //        var vvv = CatalogList.Count();

        //        { }



        //        //var catalogsHaveFilesMore10MB = SafeReadDirectory.EnumerateFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories)
        //        //    //.Where(f => Extensions.GetFileSize(f) > File10MB)
        //        //    .Select(s => System.IO.Path.GetDirectoryName(s))
        //        //    .GroupBy(g => g)
        //        //    .Select(s => s.Key);
        //        //    { }
        //        //var allFiles = EnumerateFiles(start_path, "*.*", SearchOption.AllDirectories);
        //        //var allFilesMore10MB = allFiles.Where(f => Extensions.GetFileSize(f) > File10MB);
        //        //});

        //        //foreach (var file in allFilesMore10MB)
        //        //{
        //        //    //CatalogList.Add()
        //        //}

        //        //var folders = Directory.GetDirectories(start_path);
        //        //foreach (var folder in folders)
        //        //{
        //        //    var files = Directory.GetFiles(folder);

        //        //    { }

        //        //    if (files.Any())
        //        //    {

        //        //    }


        //        //    //var rowViewModel = new RowViewModel(new DirectoryInfo(folder).Name, folder, _pauseEvent, _cancellationTokenSource?.Token ?? default);

        //        //    //CatalogList.Add(rowViewModel);

        //        //    //CatalogList.AddRange(GetRecursFiles(folder));
        //        //}

        //        //var files = Directory.GetFiles(start_path);
        //        //foreach (var filename in files)
        //        //{
        //        //    ls.Add("Файл: " + filename);
        //        //}
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //    }
        //    //return ls;
        //}

        //private static void AddFiles(string path, IList<string> files)
        //{
        //    try
        //    {
        //        Directory.GetFiles(path)
        //            .ToList()
        //            .ForEach(s => files.Add(s));

        //        Directory.GetDirectories(path)
        //            .ToList()
        //            .ForEach(s => AddFiles(s, files));
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        // ok, so we are not allowed to dig into that directory. Move on.
        //    }
        //}

        //public IEnumerable<string> GetFileList1111(string fileSearchPattern, string rootFolderPath)
        //{
        //    Queue<string> pending = new Queue<string>();
        //    pending.Enqueue(rootFolderPath);
        //    string[] tmp;
        //    while (pending.Count > 0)
        //    {
        //        rootFolderPath = pending.Dequeue();
        //        try
        //        {
        //            tmp = Directory.GetFiles(rootFolderPath, fileSearchPattern);
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            continue;
        //        }
        //        for (int i = 0; i < tmp.Length; i++)
        //        {
        //            yield return tmp[i];
        //        }
        //        tmp = Directory.GetDirectories(rootFolderPath);
        //        for (int i = 0; i < tmp.Length; i++)
        //        {
        //            pending.Enqueue(tmp[i]);
        //        }
        //    }
        //}

        //public static void FileIterationOne(string path)
        //{
        //    var sw = Stopwatch.StartNew();
        //    int count = 0;
        //    string[]? files = null;
        //    try
        //    {
        //        files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        Console.WriteLine("You do not have permission to access one or more folders in this directory tree.");
        //        return;
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        Console.WriteLine($"The specified directory {path} was not found.");
        //    }

        //    //var fileContents =
        //    //    from FileName in files?.AsParallel()
        //    //    let extension = System.IO.Path.GetExtension(FileName);

        //    //let Text = File.ReadAllText(FileName)
        //    //select new
        //    //{
        //    //    Text,
        //    //    FileName
        //    //};

        //    //var fileContents = files?.AsParallel().ForAll(f => f);
        //    var fileContents = files?.AsParallel().Select(f => f);

        //    try
        //    {
        //        foreach (var item in fileContents)
        //        {
        //            Debug.WriteLine($"{System.IO.Path.GetFileName(item)}");
        //            count++;
        //        }
        //    }
        //    catch (AggregateException ae)
        //    {
        //        ae.Handle(ex =>
        //        {
        //            if (ex is UnauthorizedAccessException uae)
        //            {
        //                Debug.WriteLine(uae.Message);
        //                return true;
        //            }
        //            return false;
        //        });
        //    }

        //    Console.WriteLine($"FileIterationOne processed {count} files in {sw.ElapsedMilliseconds} milliseconds");
        //}


        public static void TraverseTreeParallelForEach(string root, Action<string> action)
        {
            //Count of files traversed and timer for diagnostic output
            int fileCount = 0;
            var sw = Stopwatch.StartNew();

            // Determine whether to parallelize file processing on each folder based on processor count.
            int procCount = Environment.ProcessorCount;

            // Data structure to hold names of subfolders to be examined for files.
            Stack<string> dirs = new Stack<string>();

            if (!Directory.Exists(root))
            {
                throw new ArgumentException(
                    "The given root directory doesn't exist.", nameof(root));
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs = { };
                IEnumerable<string> files = Enumerable.Empty<string>();

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    continue;
                }

                try
                {
                    files = Directory.GetFiles(currentDir).Where(f => Extensions.GetFileSize(f) > File10MB);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    continue;
                }

                // Execute in parallel if there are enough files in the directory.
                // Otherwise, execute sequentially.Files are opened and processed
                // synchronously but this could be modified to perform async I/O.
                try
                {
                    if (files.Count() < procCount)
                    {
                        foreach (var file in files)
                        {
                            action(currentDir);
                            fileCount++;
                        }
                    }
                    else
                    {
                        Parallel.ForEach(files, () => 0,
                            (file, loopState, localCount) =>
                            {
                                action(currentDir);
                                return (int)++localCount;
                            },
                            (c) =>
                            {
                                Interlocked.Add(ref fileCount, c);
                            });
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is UnauthorizedAccessException)
                        {
                            // Here we just output a message and go on.
                            Debug.WriteLine(ex.Message);
                            return true;
                        }
                        // Handle other exceptions here if necessary...

                        return false;
                    });
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            // For diagnostic purposes.
            Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
        }


        async Task<List<string>> DirSearch(string sDir)
        {
            var files = new List<string>();

            await Task.Run(async () =>
            {
                try
                {
                    foreach (string f in Directory.GetFiles(sDir))
                    {
                        files.Add(f);

                        var rowViewModel = new RowViewModel(f, f, _pauseEvent, _cancellationTokenSource?.Token ?? default);
                        ////rowViewModel.Calculate();
                        App.Current?.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel)); // ToDo: _cancellationTokenSource.Token
                    }

                    foreach (string d in Directory.GetDirectories(sDir))
                    {
                        var hhhh = await Task.Run(() => DirSearch(d));
                        files.AddRange(hhhh);
                    }
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine(excpt.Message);
                }
            });

            return files;
        }

        private async Task ProcessRead(string path)
        {
            await Task.Run(() =>
            {
                var fileEntries = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                //var dirEntries = Directory.EnumerateDirectories(path, "*.*");

                int count = 0;
                foreach (string fname in fileEntries)
                {
                    try
                    {
                        count++;
                        //string text = File.ReadAllText(fname);
                        Debug.WriteLine(fname);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }

                Debug.WriteLine(count);
            });
        }


        public async void Search()
        {
            //var catalogsHaveFilesMore10MB = SafeReadDirectory.EnumerateFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories)
            //    //.Where(f => Extensions.GetFileSize(f) > File10MB)
            //    .Select(s => System.IO.Path.GetDirectoryName(s))
            //    .GroupBy(g => g)
            //    .Select(s => s.Key);

            //var hhh = catalogsHaveFilesMore10MB.Count();


            //string[] allfiles = Directory.GetFiles(DriveSelectedItem);
            //var allFiles = Directory.GetFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories);

            //var fff = allFiles.Count();
            //CatalogList.CollectionChanged += CatalogList_CollectionChanged;



            //var count = ls.Count();

            //var directory = new DirectoryInfo(DriveSelectedItem);
            //var files = directory.GetFiles("*.*", SearchOption.AllDirectories);

            //var fff = files.Count();


            //try
            //{
            //    var files = Directory.GetFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories);
            //}
            //catch (Exception)
            //{

            //}

            //var files = new List<string>();
            //AddFiles(DriveSelectedItem, files);

            //FileIterationOne(DriveSelectedItem);

            //GetFileList1111("*.*", DriveSelectedItem);




            //var gggg = await Task.Run(() => Directory.EnumerateFiles(DriveSelectedItem, "*.*").GetEnumerator());

            // UI thread
            //var filePaths = await Task.Run(() =>
            //{
            //    // Task.Run ensures that EnumerateFilesAsync is not called from a UI thread and that the
            //    // synchronization context that ConfigureMoveNext captures will post work to the thread pool.
            //    return Directory.EnumerateFiles(DriveSelectedItem, "*.*").ConfigureMoveNext(true);
            //});

            //await foreach (var filePath in filePaths)
            //{
            //    // List the file in the UI.
            //}

            //await ProcessRead(DriveSelectedItem);

            var gggg = await DirSearch(DriveSelectedItem);

            { }

            //using (var e = await Task.Run(() => Directory.EnumerateFiles(DriveSelectedItem, "*.*").GetEnumerator()))
            //{
            //    while (await Task.Run(() => e.MoveNext()))
            //    {
            //        Debug.WriteLine(e.Current);
            //    }
            //}




            //try
            //{
            //    //await Task.Run(() =>
            //    //{
            //        TraverseTreeParallelForEach(DriveSelectedItem, (f) =>
            //        {
            //            try
            //            {

            //                //var rowViewModel = new RowViewModel(System.IO.Path.GetDirectoryName(f), f, _pauseEvent, _cancellationTokenSource?.Token ?? default);
            //                //rowViewModel.Calculate();
            //                //App.Current?.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel)); // ToDo: _cancellationTokenSource.Token
            //            }
            //            catch (Exception e)
            //            {
            //                Debug.WriteLine(e.Message);
            //            }
            //        });
            //    //}); // ToDo: _cancellationTokenSource.Token
            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //}









            { }

            return;

            IsRunSearch = !IsRunSearch;

            if (!IsRunSearch)
            {
                ResetSearch();
            }
            else
            {
                SearchOrResetText = ResetText;
                PauseResumeText = PauseText;

                TotalFileSize = 0;
                TotalFileCount = 0;

                _cancellationTokenSource = new CancellationTokenSource();
                _pauseEvent = new ManualResetEvent(true);

                //GetRecursFiles(DriveSelectedItem);
                

                //string[] subDirs = { };
                //string[] files = { };


                //Check();

                //CatalogList.CollectionChanged += CatalogList_CollectionChanged;

                //try
                //{
                //    subDirs = Directory.GetDirectories(DriveSelectedItem);

                //    var totalCalculatingTask = Task.Run(CalculatingTotalFilesAndTheirSize, _cancellationTokenSource.Token);
                //    var calculatingInEachRow = Task.Run(CalculatingInFileCountAndSizeEachCatalog, _cancellationTokenSource.Token);

                //    await Task.WhenAll(totalCalculatingTask, calculatingInEachRow);
                //}
                //catch (Exception e)
                //{
                //    Debug.WriteLine(e.Message);
                //}
            }
        }

        //private void Check()
        //{
        //    //GetFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories);
        //}

        //public static IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*",
        //    SearchOption searchOption = SearchOption.AllDirectories)
        //{
        //    //if (searchOption == SearchOption.TopDirectoryOnly)
        //    //    return Directory.EnumerateFiles(path, searchPattern).ToList();

        //    //var files = new List<string>(GetDirectories(path, searchPattern));

        //    //foreach (var file in files)
        //    //{
        //    //    files.AddRange(GetDirectories(file, searchPattern));
        //    //}

        //    try
        //    {
        //        var dirFiles = Enumerable.Empty<string>();

        //        if (searchOption == SearchOption.AllDirectories)
        //        {
        //            dirFiles = Directory.EnumerateDirectories(path)
        //                                .SelectMany(x => EnumerateFiles(x, searchPattern, searchOption));
        //        }



        //        //var rowViewModel = new RowViewModel(new DirectoryInfo(catalog).Name, catalog, _pauseEvent, _cancellationTokenSource.Token);

        //        //rowViewModel.Calculate();

        //        //App.Current.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel));

        //        return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
        //    }
        //    catch (Exception)
        //    {
        //        return Enumerable.Empty<string>();
        //    }

        //    return files;
        //}

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.EnumerateFiles(path, searchPattern).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        private void CalculatingInFileCountAndSizeEachCatalog()
        {
            // First iterate through all files, then filter more than 10 MB, group the directory and display it in the catalog list
            var catalogsHaveFilesMore10MB = SafeReadDirectory.EnumerateFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories)
                //.Where(f => Extensions.GetFileSize(f) > File10MB)
                .Select(s => System.IO.Path.GetDirectoryName(s))
                .GroupBy(g => g)
                .Select(s => s.Key);

            foreach (var catalog in catalogsHaveFilesMore10MB)
            {
                try
                {
                    if (_cancellationTokenSource?.IsCancellationRequested ?? true)
                        break;

                    _pauseEvent.WaitOne();

                    var rowViewModel = new RowViewModel(new DirectoryInfo(catalog).Name, catalog, _pauseEvent, _cancellationTokenSource.Token);

                    rowViewModel.Calculate();

                    App.Current.Dispatcher.Invoke(() => CatalogList.Add(rowViewModel));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        public void PauseOrResume()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _pauseEvent.Reset();
            }
            else
            {
                _pauseEvent.Set();
            }

            PauseResumeText = _isPaused ? ResumeText : PauseText;
        }

        private void CalculatingTotalFilesAndTheirSize()
        {
            var filesAll = SafeReadDirectory.EnumerateFiles(DriveSelectedItem, "*.*", SearchOption.AllDirectories);

            foreach (var file in filesAll)
            {
                if (_cancellationTokenSource?.IsCancellationRequested ?? true)
                    break;

                if (!_pauseEvent.SafeWaitHandle.IsClosed)
                    _pauseEvent.WaitOne();

                TotalFileCount++;
                TotalFileSize += new FileInfo(file).Length;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _pauseEvent?.Close();
            _pauseEvent?.Dispose();
            _pauseEvent = null;
        }

        public void ResetSearch()
        {
            _isPaused = false;

            CatalogList.Clear();

            SearchOrResetText = SearchText;
            PauseResumeText = EmptyText;

            _pauseEvent?.Close();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
