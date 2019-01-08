﻿using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using System.Windows.Threading;
using Pahkat.Models;
using Pahkat.Service;
using Pahkat.Sdk;

namespace Pahkat.UI.Main
{
    public class DownloadListItem : INotifyPropertyChanged, IEquatable<DownloadListItem>
    {
        public bool Equals(DownloadListItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DownloadListItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key != null ? Key.GetHashCode() : 0) * 397);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public readonly AbsolutePackageKey Key;
        public readonly Package Model;
        private long _downloaded;
        
        public DownloadListItem(AbsolutePackageKey key, Package package)
        {
            Key = key;
            Model = package;
        }

        public string Title => Model.NativeName;
        public string Version => Model.Version;
        public long FileSize => Model.WindowsInstaller.Size;
        public long Downloaded
        {
            get => _downloaded;
            set
            {
                _downloaded = value;
                
                // Workaround for WPF bug where only one property change event can be
                // fired per setter being used... :|
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Status"));
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs("Downloaded"));
                });
            }
        }

        public string Status
        {
            get
            {
                if (_downloaded < 0)
                {
                    return Strings.DownloadError;
                }
                
                if (_downloaded == 0)
                {
                    return Strings.Downloading;
                }
                
                if (_downloaded < FileSize)
                {
                    return Util.Util.BytesToString(Downloaded);
                }

                return Strings.Downloaded;
            }
        }
    }
}