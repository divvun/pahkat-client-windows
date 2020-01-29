﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Pahkat.Sdk;

namespace Pahkat.UI.Main
{
    public struct InstallSaveState
    {
        public bool IsCancelled;
        public bool RequiresReboot;
    }

    public class InstallPagePresenter
    {
        private readonly IInstallPageView _view;
        private readonly Transaction _transaction;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancelSource;
        private readonly string _stateDir;
        
        public InstallPagePresenter(IInstallPageView view,
            Transaction transaction,
            IScheduler scheduler)
        {
            _view = view;
            _transaction = transaction;
            _scheduler = scheduler;

            _cancelSource = new CancellationTokenSource();

            // todo: This uses the common documents folder to share the installation state
            //       and is no way optimal. Since the user might change when asking for
            //       administrative rights there are few temporary folders available to the
            //       non-admin user. For future releases, find a better folder.
            var tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            _stateDir = Path.Combine(tmpPath, "Pahkat", "state");
        }

        public void SaveResultsState(InstallSaveState state)
        {
            Directory.CreateDirectory(_stateDir);
            File.WriteAllText(ResultsPath, JsonConvert.SerializeObject(state));
        }

        public string ResultsPath => Path.Combine(_stateDir, "results.json");

        public InstallSaveState ReadResultsState()
        {
            return JsonConvert.DeserializeObject<InstallSaveState>(File.ReadAllText(ResultsPath));
        }

        private IDisposable PrivilegedStart()
        {
            var app = (PahkatApp) Application.Current;
            var actions = _transaction.Actions();
            _view.SetTotalPackages(actions.Count);

            var keys = new HashSet<PackageKey>(actions.Select((x) => x.Id));
            var packages = new Dictionary<PackageKey, Package>();
            
            // Cache the packages in advance
            foreach (var repo in app.PackageStore.RepoIndexes())
            {
                var copiedKeys = new HashSet<PackageKey>(keys);
                foreach (var key in copiedKeys)
                {
                    var package = repo.Package(key);
                    if (package != null)
                    {
                        keys.Remove(key);
                        packages[key] = package;
                    }
                }
            }

            var requiresReboot = false;

            return _transaction.Process()
                .Delay(TimeSpan.FromSeconds(0.5))
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(_scheduler)
                .Subscribe((evt) =>
            {
                var action = _transaction.Actions().First((x) => x.Id.Equals(evt.PackageKey));
                var package = packages[evt.PackageKey];
                var installer = package.WindowsInstaller;
                
                switch (evt.Event)
                {
                    case PackageEventType.Installing:
                        _view.SetStarting(action.Action, package);
                        if (installer != null && installer.RequiresReboot)
                        {
                            requiresReboot = true;
                        }
                        break;
                    case PackageEventType.Uninstalling:
                        _view.SetStarting(action.Action, package);
                        if (installer != null && installer.RequiresUninstallReboot)
                        {
                            requiresReboot = true;
                        }
                        break;
                    case PackageEventType.Completed:
                        _view.SetEnding();
                        break;
                    case PackageEventType.Error:
                        MessageBox.Show(Strings.ErrorDuringInstallation, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            },
            _view.HandleError,
            () => {
                if (_cancelSource.IsCancellationRequested)
                {
                    this._view.ProcessCancelled();
                }
                else
                {
                    _view.ShowCompletion(false, requiresReboot);
                }
            });
        }

        public IDisposable Start()
        {
            if (!Util.Util.IsAdministrator())
            {
                Directory.CreateDirectory(_stateDir);
                var jsonPath = Path.Combine(_stateDir, "install.json");
                var resultsPath = Path.Combine(_stateDir, "results.json");
                try
                {
                    File.Delete(resultsPath);
                }
                catch (Exception)
                {
                    // ignored
                }

                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(_transaction.Actions()));
                _view.RequestAdmin(jsonPath);

                return _view.OnCancelClicked().Subscribe(_ =>
                {
                    _cancelSource.Cancel();
                    _view.ProcessCancelled();
                    _view.ShowCompletion(true, false);
                });
            }
            else
            {
                return PrivilegedStart();
            }
        }
    }
}