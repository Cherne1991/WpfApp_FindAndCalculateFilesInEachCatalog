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

        private long _fileSize;

        public long FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
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
    }
}
