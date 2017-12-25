﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Bahkat.Service;
using System.Collections.ObjectModel;
using Bahkat.Models;

namespace Bahkat.UI.Main
{
    public class DownloadListItem : INotifyPropertyChanged, IEquatable<DownloadListItem>
    {
        public bool Equals(DownloadListItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Model.Package.Equals(other.Model.Package);
        }

        public override int GetHashCode()
        {
            return Model.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public readonly PackageProgress Model;
        private long _downloaded;
        
        public DownloadListItem(PackageProgress package)
        {
            Model = package;
        }

        public string Title => Model.Package.NativeName;
        public string Version => Model.Package.Version;
        public long FileSize => Model.Package.Installer.Size;
        public long Downloaded
        {
            get => _downloaded;
            set
            {
                _downloaded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Downloaded"));
            }
        }
        public string Status
        {
            get
            {
                if (Downloaded < 0)
                {
                    return Strings.DownloadError;
                }
                
                if (Downloaded == 0)
                {
                    return Strings.Downloading;
                }
                
                if (Downloaded < FileSize)
                {
                    return Util.Util.BytesToString(Downloaded);
                }

                return Strings.Downloaded;
            }
        }
    }
    
    public class DownloadPagePresenter
    {
        private ObservableCollection<DownloadListItem> _listItems =
            new ObservableCollection<DownloadListItem>();
        
        private readonly IDownloadPageView _view;
        private readonly PackageStore _pkgStore;
        private readonly IPackageService _pkgServ;
        private readonly CancellationTokenSource _cancelSource;
        
        public DownloadPagePresenter(IDownloadPageView view, PackageStore pkgStore, IPackageService pkgServ)
        {
            _view = view;
            _pkgStore = pkgStore;
            _pkgServ = pkgServ;
            
            _cancelSource = new CancellationTokenSource();
        }

        private void UpdateProgress(object sender, DownloadProgressChangedEventArgs args)
        {
            var package = (PackageProgress) sender;

            _listItems
                .First(x => Equals(package, x.Model))
                .Downloaded = args.BytesReceived;
        }

        private PackageProgress CreatePackageProgress(Package package)
        {
            var prog = new PackageProgress()
            {
                Package = package
            };
            prog.Progress = (sender, e) => UpdateProgress(prog, e);
            return prog;
        }

        public IDisposable Start()
        {
            _view.InitProgressList(_listItems);

            var cancel = _view.OnCancelClicked()
                .Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.DownloadCancelled();
                });

            var downloader = _pkgStore.State
                .Select(x => x.SelectedPackages)
                .Take(1)
                .Select(x =>
                {
                    var packages = x.Select(CreatePackageProgress).ToArray();
                    
                    foreach (var item in packages)
                    {
                        _listItems.Add(new DownloadListItem(item));
                    }

                    return packages;
                })
                .Select(packages => _pkgServ.Download(packages, 3, _cancelSource.Token))
                .Switch()
                .ToArray()
                .Subscribe(_view.StartInstallation, _view.HandleError);

            return new CompositeDisposable(downloader, cancel);
        }
    }
}