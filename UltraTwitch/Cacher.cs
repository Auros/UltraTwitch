using IPA.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UltraTwitch
{
    public static class Cacher
    {
        private static readonly string _coverCachePath = Path.Combine(UnityGame.UserDataPath, "UltraTwitch", "Cache", "Covers");
        private static readonly string _userProfileCachePath = Path.Combine(UnityGame.UserDataPath, "UltraTwitch", "Cache", "Profiles");
        private static readonly string _requestHistoryPath = Path.Combine(UnityGame.UserDataPath, "UltraTwitch", "Cache", "history.csv");

        private static readonly Dictionary<string, string> _cachedCoverLocations = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _cachedUserProfilePictures = new Dictionary<string, string>();
        private static readonly Dictionary<string, Texture2D> _cachedCoverTextures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> _cachedUserProfileTextures = new Dictionary<string, Texture2D>();

        public static void Load()
        {
            Directory.CreateDirectory(_coverCachePath);
            Directory.CreateDirectory(_userProfileCachePath);

            FileInfo[] covers = Directory.EnumerateFiles(_coverCachePath).Select(x => new FileInfo(x)).ToArray();
            for (int i = 0; i < covers.Length; i++)
            {
                _cachedCoverLocations.Add(covers[i].Name.Replace(".png", ""), covers[i].FullName);
            }


            string userProfileDBPath = Path.Combine(_userProfileCachePath, "cache.csv");
            if (!File.Exists(userProfileDBPath))
            {
                var fs = new FileStream(userProfileDBPath, FileMode.Create);
                fs.Dispose();
            }
            else
            {
                string line;
                StreamReader f = new StreamReader(userProfileDBPath);
                while ((line = f.ReadLine()) != null)
                {
                    var s = line.Split(new char[] { ',' });
                    if (!_cachedUserProfilePictures.ContainsKey(s[0]))
                        _cachedUserProfilePictures.Add(s[0], s[1]);
                }
                f.Close();
            }
        }

        public static void Save()
        {
            var values = _cachedUserProfilePictures;
            string userProfileDBPath = Path.Combine(_userProfileCachePath, "cache.csv");
            if (!File.Exists(userProfileDBPath))
            {
                Directory.CreateDirectory(userProfileDBPath);
                var fs = new FileStream(userProfileDBPath + "/" + userProfileDBPath, FileMode.Create);
                fs.Dispose();
            }
            string[] lines = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                lines[i] = $"{values.Keys.ToArray()[i]},{values.Values.ToArray()[i]}";
            File.WriteAllLines(userProfileDBPath, lines);
        }

        public static Texture2D LoadCachedCover(string key)
        {
            if (_cachedCoverTextures.ContainsKey(key))
                return _cachedCoverTextures[key];
            return null;
        }

        public static async Task<Texture2D> LoadCover(string key)
        {
            if (_cachedCoverTextures.ContainsKey(key))
                return _cachedCoverTextures[key];

            if (_cachedCoverLocations.ContainsKey(key))
            {
                // Load from file
                var imgBytes = File.ReadAllBytes(_cachedCoverLocations[key]);
                if (UnityGame.OnMainThread)
                {
                    Texture2D texx = BeatSaberMarkupLanguage.Utilities.LoadTextureRaw(imgBytes);

                    _cachedCoverTextures.Add(key, texx);
                    return texx;
                }
                return null;
            }

            var bytes = await (await Plugin.BeatSaver.Key(key)).FetchCoverImage();

            var filePath = Path.Combine(_coverCachePath, key + ".png");
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
            if (!_cachedCoverLocations.ContainsKey(key))
                _cachedCoverLocations.Add(key, filePath);

            if (UnityGame.OnMainThread)
            {
                Texture2D tex = BeatSaberMarkupLanguage.Utilities.LoadTextureRaw(bytes);
                if (!_cachedCoverTextures.ContainsKey(key))
                    _cachedCoverTextures.Add(key, tex);
                return tex;
            }
            return null;
        }

        public static async Task<Texture2D> LoadUserProfile(TwitchUserData userData)
        {
            if (_cachedUserProfilePictures.ContainsKey(userData.ID))
            {
                if (_cachedUserProfilePictures[userData.ID] != userData.ProfileImageURL)
                {
                    // Download the new image
                    _cachedUserProfilePictures.Remove(userData.ID);
                    var bytes = await Plugin.Client.DownloadImage(_cachedUserProfilePictures[userData.ID], Plugin.GlobalCTS.Token);
                    byte[] newBytes = CacheUserProfile(userData, bytes);
                    Texture2D tex = BeatSaberMarkupLanguage.Utilities.LoadTextureRaw(newBytes);
                    _cachedUserProfileTextures.Add(userData.ID, tex);
                    return tex;
                }
                if (!_cachedUserProfileTextures.ContainsKey(userData.ID))
                {
                    Plugin.Log.Info("Loading cached profile texture");
                    var imgBytes = File.ReadAllBytes(_cachedUserProfilePictures[userData.ID]);
                    Texture2D tex = BeatSaberMarkupLanguage.Utilities.LoadTextureRaw(imgBytes);
                    _cachedUserProfileTextures.Add(userData.ID, tex);

                    return tex;
                }
            }
            else
            {
                _cachedUserProfilePictures.Add(userData.ID, userData.ProfileImageURL);
                var bytes = await Plugin.Client.DownloadImage(_cachedUserProfilePictures[userData.ID], Plugin.GlobalCTS.Token);
                byte[] newBytes = CacheUserProfile(userData, bytes);
                Texture2D tex = BeatSaberMarkupLanguage.Utilities.LoadTextureRaw(newBytes);
                _cachedUserProfileTextures.Add(userData.ID, tex);
                return tex;
            }
            return null;
        }

        public static byte[] CacheUserProfile(TwitchUserData data, byte[] image)
        {
            var filePath = Path.Combine(_userProfileCachePath, data.ID + ".png");
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(image, 0, image.Length);
            }
            if (!_cachedUserProfilePictures.ContainsKey(data.ID))
                _cachedUserProfilePictures.Add(data.ID, data.ProfileImageURL);
            return null;
        }

        public static void AddProfileTexture(string key, Texture2D tex)
        {
            if (!_cachedUserProfileTextures.ContainsKey(key))
                _cachedUserProfileTextures.Add(key, tex);
        }

        public static IEnumerator LoadTextureCoroutine(string spritePath, Action<Texture2D> done)
        {
            Texture2D tex;


            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(spritePath))
            {
                yield return www.SendWebRequest();
                if (www.isHttpError || www.isNetworkError)
                {
                    Plugin.Log.Error("Connection Error: " + spritePath);
                }
                else
                {
                    tex = DownloadHandlerTexture.GetContent(www);
                    tex.wrapMode = TextureWrapMode.Clamp;

                    yield return new WaitForSeconds(.01f);
                    done?.Invoke(tex);
                }
            }
        }
    }
}
