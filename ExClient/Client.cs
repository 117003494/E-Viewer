﻿using ExClient.Internal;
using ExClient.Settings;
using ExClient.Status;
using Opportunity.MvvmUniverse;
using System;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace ExClient
{
    public partial class Client : ObservableObject
    {
        public static Client Current { get; } = new Client();

        private Client()
        {
            var httpFilter = new HttpBaseProtocolFilter { AllowAutoRedirect = false };
            httpFilter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            this.CookieManager = httpFilter.CookieManager;
            this.HttpClient = new MyHttpClient(this, new HttpClient(new RedirectFilter(httpFilter)));
            if (NeedLogOn)
                ClearLogOnInfo();
            else
            {
                this.initTask = Task.Run(async () =>
                {
                    var backup = GetLogOnInfo();
                    try
                    {
                        await refreshCookieAndSettings();
                        await refreshHathPerks();
                    }
                    catch (Exception)
                    {
                        RestoreLogOnInfo(backup);
                    }
                });
            }
        }

        private Task initTask;

        internal HttpCookieManager CookieManager { get; }

        internal MyHttpClient HttpClient { get; }

        internal DomainProvider Uris => this.Host == HostType.ExHentai ? DomainProvider.Ex : DomainProvider.Eh;

        private HostType host;
        public HostType Host
        {
            get => this.host;
            set
            {
                Set(nameof(Settings), ref this.host, value);
                if (this.initTask.IsCompleted)
                    this.initTask = refreshCookieAndSettings();
            }
        }

        public SettingCollection Settings => this.Uris.Settings;


        private readonly FavoriteCollection favotites = new FavoriteCollection();
        public FavoriteCollection Favorites => NeedLogOn ? null : this.favotites;

        private readonly UserStatus userStatus = new UserStatus();
        public UserStatus UserStatus => NeedLogOn ? null : this.userStatus;

        private readonly TaggingStatistics taggingStatistics = new TaggingStatistics();
        public TaggingStatistics TaggingStatistics => NeedLogOn ? null : this.taggingStatistics;
    }
}
