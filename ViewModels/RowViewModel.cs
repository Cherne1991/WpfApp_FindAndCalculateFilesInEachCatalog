using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp_FindAndCalculateFilesInEachCatalog.ViewModels
{
    internal class RowViewModel : ViewModelBase
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ManualResetEvent _pauseEvent;
        private int _procCount = Environment.ProcessorCount;

        public RowViewModel(string directoryName, string fullPath, ManualResetEvent pauseEvent, CancellationToken cancellationToken)
        {
            _directoryName = directoryName;
            _fullPath = fullPath;
            _pauseEvent = pauseEvent;
            _cancellationToken = cancellationToken;
        }

        private string _directoryName;

        public string DirectoryName
        {
            get => _directoryName;
            set
            {
                _directoryName = value;
                OnPropertyChanged(nameof(DirectoryName));
            }
        }

        private string _fullPath;

        public string FullPath
        {
            get => _fullPath;
            set
            {
                _directoryName = value;
                OnPropertyChanged(nameof(FullPath));
            }
        }

        private long _totalSize;

        public long TotalSize
        {
            get => _totalSize;
            set
            {
                _totalSize = value;
                OnPropertyChanged(nameof(TotalSize));
            }
        }

        private long _fileCount;

        public long FileCount
        {
            get => _fileCount;
            set
            {
                _fileCount = value;
                OnPropertyChanged(nameof(FileCount));
            }
        }

        private double _percentOfCalculating;

        public double PercentOfCalculating
        {
            get => _percentOfCalculating;
            set
            {
                _percentOfCalculating = value;
                OnPropertyChanged(nameof(PercentOfCalculating));
            }
        }

        public async void Calculate()
        {
            try
            {
                await Task.Run(() =>
                {
                    var files = SafeReadDirectory.EnumerateFiles(_fullPath, "*.*", SearchOption.AllDirectories);

                    //TraditionalFilesCountAndSize(files);

                    PararellCalculatingFilesCountAndSize(files);
                }, _cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void TraditionalFilesCountAndSize(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                if (!_pauseEvent.SafeWaitHandle.IsClosed)
                    _pauseEvent.WaitOne();

                FileCount++;
                TotalSize += Extensions.GetFileSize(file);
            }
        }

        private void PararellCalculatingFilesCountAndSize(IEnumerable<string> files)
        {
            if (files.Count() < _procCount)
            {
                foreach (var file in files)
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

                    if (!_pauseEvent.SafeWaitHandle.IsClosed)
                        _pauseEvent.WaitOne();

                    FileCount++;
                    TotalSize += Extensions.GetFileSize(file);
                }
            }
            else
            {
                ParallelOptions options = new()
                {
                    CancellationToken = _cancellationToken,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                };

                //options.CancellationToken.Register(() => Debug.WriteLine("PararellSeacrhFiles -> Parallel -> Cancelled"));

                try
                {
                    Parallel.ForEach(files, options,
                        (file, loopState, localCount) =>
                        {
                            if (_cancellationToken.IsCancellationRequested)
                            {
                                loopState.Break();
                                return;
                            }

                            if (!_pauseEvent.SafeWaitHandle.IsClosed)
                                _pauseEvent.WaitOne();

                            FileCount++;
                            TotalSize += Extensions.GetFileSize(file);
                        });
                }
                catch (Exception e)
                {
                    //Debug.WriteLine("PararellSeacrhFiles -> Parallel -> " + e.Message);
                }
            }
        }
    }
}
