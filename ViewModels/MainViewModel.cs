using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public async void Search()
        {
            await Task.Run(async () =>
            {
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

                    try
                    {
                        var totalCalculatingTask = Task.Run(CalculatingTotalFilesAndTheirSize, _cancellationTokenSource.Token);
                        var calculatingInEachRow = Task.Run(CalculatingInFileCountAndSizeEachCatalog, _cancellationTokenSource.Token);

                        await Task.WhenAll(totalCalculatingTask, calculatingInEachRow);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            });
        }

        private static Func<string, bool> GetFileSizeMode10MB()
        {
            return file => GetFileSize(file) > File10MB;
        }

        private static long GetFileSize(string file)
        {
            try
            {
                return GetFileLength(file);
            }
            catch
            {
                return new FileInfo(file).Length;
            }
        }

        public static long GetFileLength(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return fileStream.Length;
            }
        }

        private void CalculatingInFileCountAndSizeEachCatalog()
        {
            // First iterate through all files, then filter more than 10 MB, group the directory and distinct directories, it in the catalog list
            var dirHasLeastOneFileMore10MBList = SafeReadDirectory.EnumerateFiles(DriveSelectedItem)
                                                        .Where(GetFileSizeMode10MB())
                                                        .Select(s => System.IO.Path.GetDirectoryName(s))
                                                        .Distinct();

            foreach (var dir in dirHasLeastOneFileMore10MBList)
            {
                if (_cancellationTokenSource?.IsCancellationRequested ?? true)
                    break;

                _pauseEvent.WaitOne();

                var rowViewModel = new RowViewModel(new DirectoryInfo(dir ?? "").Name, dir ?? "", _pauseEvent, _cancellationTokenSource?.Token ?? default);

                rowViewModel.Calculate();

                App.Current?.Dispatcher?.Invoke(() => CatalogList.Add(rowViewModel));
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

            App.Current.Dispatcher.Invoke(() => CatalogList.Clear());

            SearchOrResetText = SearchText;
            PauseResumeText = EmptyText;

            _pauseEvent?.Close();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
