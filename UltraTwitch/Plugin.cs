using IPA;
using System;
using Modject;
using ChatCore;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using BeatSaverSharp;
using UltraTwitch.UI;
using Newtonsoft.Json;
using System.Threading;
using IPA.Config.Stores;
using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using ChatCore.Models.Twitch;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using UltraTwitch.OnyxRequest;
using ChatCore.Services.Twitch;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using IPAConfig = IPA.Config.Config;
using IPALogger = IPA.Logging.Logger;
using BeatSaberMarkupLanguage.Settings;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage;
using UnityEngine.UI;

namespace UltraTwitch
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static FieldAccessor<TwitchService, IUserAuthProvider>.Accessor Yoinker = FieldAccessor<TwitchService, IUserAuthProvider>.GetAccessor("_authManager");
        internal static Dictionary<string, TwitchUserData> UserDataCache { get; private set; } = new Dictionary<string, TwitchUserData>();
        internal static Action<TwitchService, TwitchMessage, UltraTwitchUser> TwitchMessageReceived { get; set; }
        internal static CancellationTokenSource GlobalCTS { get; set; } = new CancellationTokenSource();
        internal static Action<TwitchUserData, UltraTwitchUser, Texture2D> UserJoinedToday { get; set; }
        internal static Version Version { get; private set; } = new Version(0, 3, 0);
        internal static ChatCoreInstance ChatCoreInstance { get; private set; }
        internal static TwitchService TwitchService { get; private set; }
        internal static BeatSaver BeatSaver { get; private set; }
        internal static WebClient Client { get; private set; }
        internal static Plugin Instance { get; private set; }
        internal static string ClientID { get; private set; }
        internal static Config Config { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static string OAuth { get; private set; }
        internal static Thread MainThread { get; private set; }

        [Init]
        public Plugin(IPALogger logger, IPAConfig conf)
        {
            Instance = this;
            Log = logger;
            Config = conf.Generated<Config>();
        }

        [OnEnable]
        public async void Enabled()
        {
            MainThread = Thread.CurrentThread;

            Client = new WebClient();
            ChatCoreInstance = ChatCoreInstance.Create();
            TwitchService = ChatCoreInstance.RunTwitchServices();

            bool loginStatus = await Login();

            if (!loginStatus)
            {
                Client = null;
                ChatCoreInstance.StopAllServices();
                TwitchService = null;
                ChatCoreInstance = null;

                throw new Exception("Could not connect to Twitch");
            }

            var currentUser = await GetUserInfo();

            if (string.IsNullOrEmpty(currentUser.ID))
            {
                Client = null;
                ChatCoreInstance.StopAllServices();
                TwitchService = null;
                ChatCoreInstance = null;

                throw new Exception("Could not find user profile");
            }
            
            Cacher.Load();
            UserDataCache = new Dictionary<string, TwitchUserData>();
            //UserDataCache.Add(currentUser.ID, currentUser);
            BeatSaver = new BeatSaver(new HttpOptions()
            {
                ApplicationName = "UltraTwitch",
                Version = Version,
                Agents = new ApplicationAgent[] { new ApplicationAgent("UltraTwitch", Version) }
            });
            SceneManager.activeSceneChanged += ActiveSceneChanged;
            Injector.RegisterMonoInstaller<UltraTwitchMenuInstaller>(InstallLocation.Menu);
            TwitchService.OnTextMessageReceived += MessageReceived;

            BSEvents.lateMenuSceneLoadedFresh += MenuSceneLoaded;
        }

        [OnDisable]
        public void Disabled()
        {
            Cacher.Save();

            BSMLSettings.instance.RemoveSettingsMenu(Resources.FindObjectsOfTypeAll<SettingsMenu>().FirstOrDefault());

            BSEvents.lateMenuSceneLoadedFresh -= MenuSceneLoaded;

            SceneManager.activeSceneChanged -= ActiveSceneChanged;

            TwitchService.OnTextMessageReceived -= MessageReceived;

            ChatCoreInstance.StopTwitchServices();
            ChatCoreInstance = null;

            Client = null;
        }

        private void MessageReceived(IChatService service, IChatMessage message)
        {
            // Emote Caching

            // Creating new users

            string id = message.AsTwitchMessage().Sender.AsTwitchUser().Id;
            UltraTwitchUser user = null;
            bool prevExisted = true;
            if (!Config.ViewerProfiles.ContainsKey(id))
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    user = new UltraTwitchUser() { ID = id };
                    if (id == "152734662") user.FullOverride = true;
                    Config.ViewerProfiles.Add(id, user);
                    prevExisted = false;
                }
            }
            else
            {
                user = Config.ViewerProfiles[id];
            }

            ProcessUser(user, prevExisted);
            TwitchMessageReceived?.Invoke(service.AsTwitchService(), message.AsTwitchMessage(), user);
        }

        private async void ProcessUser(UltraTwitchUser user, bool prevExisted)
        {
            if (user == null)
                return;

            if (UserDataCache.ContainsKey(user.ID))
                return;

            var userInfo = await GetUserInfo(user.ID);

            if (!UserDataCache.ContainsKey(user.ID))
                UserDataCache.Add(user.ID, userInfo);

            if (prevExisted && DateTime.Now < user.LastSeenDate().AddHours(6))
                return;
            Log.Notice($"Need to update {userInfo.DisplayName} ({user.ID})'s information. Updating now.");
            Texture2D tex = await Cacher.LoadUserProfile(userInfo);



            if (tex == null)
            {
                Log.Warn($"We don't have {userInfo.DisplayName} ({user.ID})'s texture! Downloading from source");
                // Fallback if for some reason we don't have the texture
                SharedCoroutineStarter.instance.StartCoroutine(Cacher.LoadTextureCoroutine(userInfo.ProfileImageURL, (ctx) =>
                {
                    Cacher.AddProfileTexture(userInfo.ID, ctx);
                    UserJoinedToday?.Invoke(userInfo, user, ctx);
                }));
            }
            else
            {
                UserJoinedToday?.Invoke(userInfo, user, tex);
            }
        }

        public async Task<bool> Login()
        {
            var service = TwitchService;
            var token = Yoinker(ref service).Credentials.Twitch_OAuthToken;
            OAuth = token.Replace("oauth:", "");

            var response = await Client.GetAsync("https://id.twitch.tv/oauth2/validate", GlobalCTS.Token, new AuthenticationHeaderValue("OAuth", OAuth));

            if (response.IsSuccessStatusCode)
            {
                var responsestring = response.ContentToString();

                var responsejson = JSONNode.Parse(responsestring);

                if (responsejson["client_id"])
                {
                    ClientID = responsejson["client_id"];
                    return true;
                }
            }

            return false;
        }

        public async Task<TwitchUserData> GetUserInfo(string id = null)
        {
            
            string url = "https://api.twitch.tv/helix/users" + (id == null ? "" : "?id=" + id);

            WebResponse response = await Client.GetTwitchAsync(url, GlobalCTS.Token, OAuth, ClientID);
            Plugin.Log.Info(response.ContentToString());
            if (response.IsSuccessStatusCode)
            {
                var responsestring = response.ContentToString();

                var responsejson = JSONNode.Parse(responsestring);

                if (responsejson["data"] != null && responsejson["data"].AsArray.Count > 0)
                {
                    var userPayload = responsejson["data"].AsArray[0];

                    TwitchUserData data = JsonConvert.DeserializeObject<TwitchUserData>(userPayload.ToString());
                    
                    return data;
                }
            }
            
            return new TwitchUserData();
        }

        private void ActiveSceneChanged(Scene @old, Scene @new)
        {
            if (@new.name == "MenuViewControllers")
            {
                CheckActiveChannel();
                BSMLSettings.instance.AddSettingsMenu(
                    "UltraTwitch",
                    "UltraTwitch.Views.settings-menu.bsml",
                    new GameObject("UltraTwitch Settings Host")
                    .AddComponent<SettingsMenu>());
            }
        }

        internal static void DestroyBots()
        {
            OnyxRequestBot[] bots = Resources.FindObjectsOfTypeAll<OnyxRequestBot>();

            for (int i = 0; i < bots.Length; i++)
            {
                UnityEngine.Object.Destroy(bots[i].gameObject);
            }
        }

        internal static void CheckActiveChannel()
        {
            if (string.IsNullOrEmpty(Config.MainChannel))
            {
                Config.MainChannel = TwitchService.Channels.Keys.LastOrDefault();
            }
            else
            {
                if (TwitchService.Channels.Keys.Contains(Config.MainChannel))
                    TwitchService.Channels.Keys.LastOrDefault();
            }
        }

        private void MenuSceneLoaded(ScenesTransitionSetupDataSO setupData)
        {
            var floatingScreen = FloatingScreen.CreateFloatingScreen(
                new Vector2(150, 70),
                false,
                new Vector3(0f, 3f, 2.4f),
                Quaternion.Euler(-15, 0, 0));

            var viewController = BeatSaberUI.CreateViewController<OverlayViewController>();
            viewController.floatingScreen = floatingScreen;
            floatingScreen.SetRootViewController(viewController, true);
            floatingScreen.GetComponent<Image>().enabled = false;
        }
    }
}
