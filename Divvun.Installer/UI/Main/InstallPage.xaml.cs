﻿using System;
using Iterable;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Divvun.Installer.Extensions;
using System.Windows.Navigation;
using Divvun.Installer.Models;
using Divvun.Installer.UI.Shared;
using Divvun.Installer.Util;
using Pahkat.Sdk;
using Pahkat.Sdk.Rpc;
using Sentry;

namespace Divvun.Installer.UI.Main
{
    public interface IInstallPageView : IPageView
    {
        // IObservable<EventArgs> OnCancelClicked();
        // void SetStarting(PackageAction action, string name, string version);
        // void SetEnding();
        // void SetTotalPackages(long total);
        // void ShowCompletion(bool isCancelled, bool requiresReboot);
        void HandleError(Exception error);
        void ProcessCancelled();
    }

    /// <summary>
    /// Interaction logic for InstallPage.xaml
    /// </summary>
    public partial class InstallPage : Page, IInstallPageView, IDisposable
    {
        private CompositeDisposable _bag = new CompositeDisposable();
        private NavigationService _navigationService;

        public InstallPage() {
            InitializeComponent();
        }

        public IObservable<EventArgs> OnCancelClicked() =>
            BtnCancel.ReactiveClick().Map(x => x.EventArgs);

        private void SetRemaining() {
            var max = PrgBar.Maximum;
            var value = PrgBar.Value;

            LblSecondary.Text = string.Format(Strings.NItemsRemaining, max - value);
        }

        private void SetCurrentItem(PackageKey packageKey) {
            var app = (PahkatApp) Application.Current;
            if (!app.CurrentTransaction.Value.IsT1) {
                return;
            }
            
            var x = app.CurrentTransaction.Value.AsT1;
            var actions = x.Actions;
            
            var position = Array.FindIndex(
                actions, x => x.Action.PackageKey.Equals(packageKey));

            PrgBar.Maximum = actions.Length;
            PrgBar.Value = position;
            PrgBar.IsIndeterminate = false;

            var action = actions[position];
            
            var fmtString = action.Action.Action == InstallAction.Install
                ? Strings.InstallingPackage
                : Strings.UninstallingPackage;
            LblPrimary.Text = string.Format(fmtString, action.NativeName(), action.Version);
            SetRemaining();
        }
        
        public void ProcessCancelled() {
            BtnCancel.IsEnabled = false;
            LblSecondary.Text = Strings.WaitingForCompletion;
        }

        public void HandleError(Exception error) {
            var app = (PahkatApp) Application.Current;
            SentrySdk.CaptureException(error);
            MessageBox.Show(error.Message,
                Strings.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public void Dispose() {
            _bag.Dispose();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            _navigationService = this.NavigationService;
            _navigationService.Navigating += NavigationService_Navigating;
            
            // Set total packages from the information we have
            var app = (PahkatApp) Application.Current;

            // Control the state of the current view
            app.CurrentTransaction.AsObservable()
                .ObserveOn(DispatcherScheduler.Current)
                .SubscribeOn(DispatcherScheduler.Current)
                // Resolve down the events to Download-related ones only
                .Filter(x => x.IsInProgressInstalling)
                .Map(x => x.AsT1.State.AsInstallState!.CurrentItem)
                .SubscribeOn(DispatcherScheduler.Current)
                .Subscribe(item => {
                    SetCurrentItem(item);
                })
                .DisposedBy(_bag);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {
            _navigationService.Navigating -= NavigationService_Navigating;
        }

        private void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) {
            if (e.NavigationMode == NavigationMode.Back) {
                e.Cancel = true;
            }
        }
    }
}