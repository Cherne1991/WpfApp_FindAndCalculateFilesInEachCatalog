using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WpfApp_FindAndCalculateFilesInEachCatalog.Models;

namespace WpfApp_FindAndCalculateFilesInEachCatalog.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private const string SearchText = "Search";
        private const string PauseText = "Pause";
        private const string ResumeText = "Resume";
        private const string SearchingText = "Searching";
        private const string StopText = "Stop";
        private const string DoneText = "Done";
        private const string EmptyText = "";
        private const int File10MB = 10 * 1024 * 1024;

        private ManualResetEvent _pauseEvent;
        private CancellationTokenSource _cancellationTokenSource, _cancellationTokenSourceForSearchState;
        private bool _isPaused = false;

        public ObservableCollection<RowViewModel> CatalogList { get; set; } = new ObservableCollection<RowViewModel>();

        public ObservableCollection<string> Drives { get; set; } = new ObservableCollection<string>();

        public MainViewModel()
        {
            InitializeDrives();

            SearchOrResetText = SearchText;
            PauseResumeText = EmptyText;
        }

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

        private string _searchState;

        public string SearchState
        {
            get => _searchState;
            set
            {
                _searchState = value;
                OnPropertyChanged(nameof(SearchState));
            }
        }

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

        public void Reset()
        {
            ResetSearch();
        }

        public async void Search()
        {
            await Task.Run(async () =>
            {
                ResetSearch();

                IsRunSearch = true;

                PauseResumeText = PauseText;
                TotalFileSize = 0;
                TotalFileCount = 0;

                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSourceForSearchState = new CancellationTokenSource();
                _pauseEvent = new ManualResetEvent(true);

                try
                {
                    var searchFilesTask = Task.Run(() => CalculatingInFileCountAndSizeEachCatalog(_cancellationTokenSource.Token));
                    var searchState = Task.Run(() => GetSearchState(_cancellationTokenSourceForSearchState.Token));

                    searchFilesTask.GetAwaiter()
                        .OnCompleted(() =>
                        {
                            SearchState = DoneText;
                            IsRunSearch = false;

                            _cancellationTokenSourceForSearchState?.Cancel();
                        });

                    await Task.WhenAll(searchFilesTask, searchState);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        private void CalculatingInFileCountAndSizeEachCatalog(CancellationToken cancellationToken)
        {
            try
            {
                var procCount = Environment.ProcessorCount;
                var dirs = new Stack<string>();

                if (!Directory.Exists(DriveSelectedItem))
                {
                    throw new ArgumentException("The given root directory doesn't exist.", nameof(DriveSelectedItem));
                }

                dirs.Push(DriveSelectedItem);

                while (dirs.Count > 0)
                {
                    var currentDir = dirs.Pop();
                    string[] subDirs = { };
                    string[] files = { };
                    long fileCount = 0;
                    long fileSize = 0;

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
                        files = Directory.GetFiles(currentDir, "*.*");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        continue;
                    }

                    if (files.Any(f => Extensions.GetFileSize(f) > File10MB))
                    {
                        var rowViewModel = new RowViewModel(new DirectoryInfo(currentDir ?? "").Name, currentDir ?? "", _pauseEvent, cancellationToken);

                        App.Current?.Dispatcher?.Invoke(() => CatalogList.Add(rowViewModel));

                        try
                        {
                            if (files.Length < procCount)
                            {
                                foreach (var file in files)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                        return;

                                    if (!_pauseEvent.SafeWaitHandle.IsClosed)
                                        _pauseEvent.WaitOne();

                                    fileCount++;
                                    fileSize += Extensions.GetFileSize(file);
                                }
                            }
                            else
                            {
                                ParallelOptions options = new()
                                {
                                    CancellationToken = cancellationToken,
                                    MaxDegreeOfParallelism = procCount,
                                };

                                Parallel.ForEach(files, options, () => new MyFileInfo(),
                                   (file, loopState, localCount, tuple) =>
                                   {
                                       if (cancellationToken.IsCancellationRequested)
                                       {
                                           loopState.Break();
                                           return new MyFileInfo();
                                       }

                                       if (!_pauseEvent.SafeWaitHandle.IsClosed)
                                           _pauseEvent.WaitOne();

                                       tuple.LocalCount++;
                                       tuple.LocalSize += Extensions.GetFileSize(file);

                                       return tuple;
                                   },
                                   tuple =>
                                   {
                                       Interlocked.Add(ref fileCount, tuple.LocalCount);
                                       Interlocked.Add(ref fileSize, tuple.LocalSize);
                                   });
                            }

                            rowViewModel.FileCount += fileCount;
                            rowViewModel.FileSize += fileSize;

                            TotalFileCount += fileCount;
                            TotalFileSize += fileSize;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }

                    foreach (string str in subDirs)
                        dirs.Push(str);
                }
            }
            catch (ArgumentException)
            {
                Debug.WriteLine(@"The directory 'C:\Program Files' does not exist.");
            }
        }

        public void PauseOrResume()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                SearchState = StopText;

                _pauseEvent.Reset();
            }
            else
            {
                SearchState = SearchingText;

                _pauseEvent.Set();
            }

            PauseResumeText = _isPaused ? ResumeText : PauseText;
        }

        private void GetSearchState(CancellationToken cancellationToken)
        {
            while (true)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (_isPaused)
                    {
                        SearchState = StopText;
                        continue;
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (!_pauseEvent?.SafeWaitHandle.IsClosed ?? false)
                        _pauseEvent?.WaitOne();

                    SearchState = SearchingText + new string('.', i);

                    Debug.WriteLine(SearchState);

                    Thread.Sleep(1000);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _cancellationTokenSourceForSearchState?.Dispose();
            _cancellationTokenSourceForSearchState = null;

            _pauseEvent?.Close();
            _pauseEvent?.Dispose();
            _pauseEvent = null;
        }

        public void ResetSearch()
        {
            _isPaused = false;
            IsRunSearch = false;

            App.Current.Dispatcher.Invoke(() => CatalogList.Clear());

            SearchOrResetText = SearchText;
            PauseResumeText = EmptyText;
            SearchState = EmptyText;

            _pauseEvent?.Close();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _cancellationTokenSourceForSearchState?.Cancel();
            _cancellationTokenSourceForSearchState?.Dispose();
            _cancellationTokenSourceForSearchState = null;
        }
    }
}
