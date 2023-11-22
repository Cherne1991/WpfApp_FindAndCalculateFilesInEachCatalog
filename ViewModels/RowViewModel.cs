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
                    var files = Directory.EnumerateFiles(_fullPath, "*.*", SearchOption.TopDirectoryOnly);

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
                long localFileCount = 0;
                long localFileSize = 0;

                ParallelOptions options = new()
                {
                    CancellationToken = _cancellationToken,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                };

                //options.CancellationToken.Register(() => Debug.WriteLine("PararellSeacrhFiles -> Parallel -> Cancelled"));

                try
                {
                    Parallel.ForEach(files, options, () => new MyFileInfo(),
                        (file, loopState, localCount, tuple) =>
                        {
                            if (_cancellationToken.IsCancellationRequested)
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
                        (tuple) =>
                        {
                            Interlocked.Add(ref localFileCount, tuple.LocalCount);
                            Interlocked.Add(ref localFileSize, tuple.LocalSize);

                            FileCount = localFileCount;
                            TotalSize = localFileSize;
                        });
                }
                catch (Exception e)
                {
                    //Debug.WriteLine("PararellSeacrhFiles -> Parallel -> " + e.Message);
                }
            }
        }

        private class MyFileInfo
        {
            public long LocalSize = -1;
            public long LocalCount = -1;
        }
    }
}
