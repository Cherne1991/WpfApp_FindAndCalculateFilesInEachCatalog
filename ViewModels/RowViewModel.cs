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

        public RowViewModel(string directoryName, ManualResetEvent pauseEvent, CancellationToken cancellationToken)
        {
            _directoryName = directoryName;
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
                    try
                    {
                        var files = SafeReadDirectory.EnumerateFiles(DirectoryName, "*.*", SearchOption.AllDirectories, true);

                        //TradionalSearchFiles(files);

                        PararellSeacrhFiles(files);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }, _cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void TradionalSearchFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                _pauseEvent.WaitOne();

                Thread.Sleep(10);

                FileCount++;
                TotalSize += Extensions.GetFileSize(file);
            }
        }

        private void PararellSeacrhFiles(IEnumerable<string> files)
        {
            if (files.Count() < _procCount)
            {
                foreach (var file in files)
                {
                    if (_cancellationToken.IsCancellationRequested)
                        return;

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
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(files, options, () => 0,
                    (file, loopState, localCount) =>
                    {
                        if (!_pauseEvent.SafeWaitHandle.IsClosed)
                            _pauseEvent.WaitOne();

                        Thread.Sleep(10);

                        FileCount++;
                        TotalSize += Extensions.GetFileSize(file);

                        return (int)++localCount;
                    },
                    _ => { });
            }
        }
    }
}
