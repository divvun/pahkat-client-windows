﻿using Pahkat.Sdk.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Pahkat.Sdk.PahkatClientException;

namespace Pahkat.Sdk
{
    public class PackageStore : Arced
    {
        internal PackageStore(IntPtr handle) : base(handle) { }

        public static PackageStore Default() => pahkat_client.pahkat_windows_package_store_default();

        public static PackageStore New(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var store = pahkat_client.pahkat_windows_package_store_new(fullPath, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return store;
        }

        public static PackageStore Load(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var store = pahkat_client.pahkat_windows_package_store_load(fullPath, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return store;
        }

        public static PackageStore NewForSelfUpdate(string path)
        {
            // We copy the file to a tmp file first so we can modify it without worry.
            var tmpFile = Path.GetTempFileName();
            File.Copy(path, tmpFile, true);
            return Load(tmpFile);
        }

        public StoreConfig Config()
        {
            var result = pahkat_client.pahkat_windows_package_store_config(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        public (PackageStatus, PackageTarget) Status(PackageKey key)
        {
            // Try user, then system.

            var userResult = pahkat_client.pahkat_windows_package_store_status(this, key, "user");
            PahkatClientException.AssertNoError();

            if (userResult > 0)
            {
                return (PackageStatusExt.FromInt(userResult), PackageTarget.User);
            }

            var sysResult = pahkat_client.pahkat_windows_package_store_status(this, key, "system");
            PahkatClientException.AssertNoError();
            return (PackageStatusExt.FromInt(sysResult), PackageTarget.System);
        }

        public void RefreshRepos()
        {
            pahkat_client.pahkat_windows_package_store_refresh_repos(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
        }

        public void ClearCache()
        {
            pahkat_client.pahkat_windows_package_store_clear_cache(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
        }

        public void ForceRefreshRepos()
        {
            pahkat_client.pahkat_windows_package_store_force_refresh_repos(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
        }

        public bool RemoveRepo(string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_remove_repo(this, url, channel, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        public bool AddRepo(string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_add_repo(this, url, channel, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        public bool UpdateRepo(uint index, string url, string channel)
        {
            var result = pahkat_client.pahkat_windows_package_store_update_repo(this, index, url, channel, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        //public Dictionary<PackageKey, PackageStatus> AllStatuses(RepoRecord repo, PackageTarget target)
        //{

        //}

        public RepositoryIndex[] RepoIndexes(bool withStatuses = true)
        {
            var result = pahkat_client.pahkat_windows_package_store_repo_indexes(this, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();

            if (withStatuses)
            {

            }

            return result;
        }

        public Package ResolvePackage(PackageKey key)
        {
            var result = pahkat_client.pahkat_windows_package_store_resolve_package(this, key, PahkatClientException.Callback);
            PahkatClientException.AssertNoError();
            return result;
        }

        public IObservable<DownloadProgress> Download(PackageKey key, PackageTarget target)
        {
            return Observable.Create<DownloadProgress>((observer) =>
            {
                // Callback for FFI
                void Callback(IntPtr rawPackageId, ulong cur, ulong max)
                {
                    if (cur < max)
                    {
                        observer.OnNext(DownloadProgress.Progress(key, cur, max));
                    }
                    else
                    {
                        observer.OnNext(DownloadProgress.Progress(key, cur, max));
                        observer.OnNext(DownloadProgress.Completed(key));
                    }
                }

                var task = new Task(() =>
                {
                    observer.OnNext(DownloadProgress.NotStarted(key));
                    observer.OnNext(DownloadProgress.Starting(key));

                    unsafe
                    {
                        var ret = pahkat_client.pahkat_windows_package_store_download(this, key, Callback, PahkatClientException.Callback);
                        try
                        {
                            PahkatClientException.AssertNoError();
                            observer.OnCompleted();
                        }
                        catch (PahkatClientException e)
                        {
                            observer.OnNext(DownloadProgress.Error(key, e.Message));
                            observer.OnError(e);
                        }
                    }
                });

                task.Start();

                return Disposable.Empty;
            });
        }
    }
}
