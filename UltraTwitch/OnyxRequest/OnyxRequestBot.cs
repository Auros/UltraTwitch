using System;
using Zenject;
using ChatCore;
using System.Linq;
using UnityEngine;
using BeatSaverSharp;
using SongDataCore.BeatStar;
using ChatCore.Models.Twitch;
using ChatCore.Services.Twitch;
using System.Collections.Generic;

namespace UltraTwitch.OnyxRequest
{
    public class OnyxRequestBot : MonoBehaviour
    {
        private Config _config;
        private TwitchChannel _channel;
        private TwitchService _service;
        private BeatStarDatabase _database;
        private List<OnyxRequest> _requestQueue;
        private Dictionary<string, Beatmap> _cachedBeatmaps;


        public void Awake()
        {
            DontDestroyOnLoad(this);
        }

        [Inject]
        public void Init(TwitchService twitch, Config config, BeatStarDatabase database)
        {
            _config = config;
            _service = twitch;
            _database = database;
            _requestQueue = new List<OnyxRequest>();
            _cachedBeatmaps = new Dictionary<string, Beatmap>();
            Plugin.TwitchMessageReceived += TwitchMessageReceived;
            _channel = _service.Channels[config.MainChannel].AsTwitchChannel();
        }

        public OnyxRequest[] RequestQueue() => _requestQueue.ToArray();

        private void TwitchMessageReceived(TwitchService service, TwitchMessage message, UltraTwitchUser user)
        {
            if (!message.Message.StartsWith(_config.Onyx.RequestPrefix))
                return;

            string[] messageSplit = message.Message.Split(' ');

            bool overrideArg = messageSplit.Any(x => x == "-a");
            bool forceArg = messageSplit.Any(x => x == "-f");

            if (messageSplit.Length < 2)
            {
                // How to use BSR
                return;
            }

            string songKey = messageSplit[1];

            int reqs = 2;
            if (message.Sender.IsBroadcaster)
                reqs = -1;
            else if (message.Sender.IsModerator)
                reqs = _config.Onyx.ModMaxRequests;
            else if (message.Sender.AsTwitchUser().IsVip)
                reqs = _config.Onyx.VIPMaxRequests;
            else if (message.Sender.AsTwitchUser().IsSubscriber)
                reqs = _config.Onyx.SubscribersMaxRequests;
            else
                reqs = _config.Onyx.NormalUsersMaxRequests;

            bool songBanned = _config.Onyx.BannedRequests.Any(x => x == songKey);
            bool requestedInSession = _config.Onyx.RequestHistory.Any(x => x.key == songKey && x.RequestDate().AddHours(_config.Onyx.SessionLength) > DateTime.Now);
            bool alreadyInQueue = _requestQueue.Any(x => x.key == songKey);
            bool notMaxed = (_requestQueue.Where(x => x.requestor.ID == user.ID).Count() <= reqs) || reqs == -1;
            bool canRequest = (user.CanRequestSongs && notMaxed && !alreadyInQueue && !songBanned) || (user.FullOverride && overrideArg);

            Plugin.Log.Info("Can Request: " + canRequest);

            if (canRequest && !(!overrideArg && requestedInSession && !_config.Onyx.AllowMultipleInSession))
            {
                AddSongToQueue(songKey, user, message.Sender.Name, forceArg && overrideArg && user.FullOverride);
            }
            else
            {
                if (!notMaxed)
                {
                    _service.SendTextMessage($"The maximum number of requests for your role is {reqs}.", _channel);
                    return;
                }
                else if (songBanned)
                {
                    _service.SendTextMessage($"The song {songKey} is banned!", _channel);
                }
                else if (requestedInSession | (!overrideArg && requestedInSession && !_config.Onyx.AllowMultipleInSession))
                {
                    _service.SendTextMessage($"The song has already been requested in the sesion.", _channel);
                    return;
                }
                else
                {
                    _service.SendTextMessage($"Could not add song to the queue", _channel);
                    return;
                }
            }
        }

        public async void AddSongToQueue(string key, UltraTwitchUser requestor, string username, bool forceToTop = false)
        {
            if (!_cachedBeatmaps.ContainsKey(key))
            {
                // Load the map if its not cached.
                Beatmap beatmap = await Plugin.BeatSaver.Key(key);
                if (beatmap == null)
                {
                    _service.SendTextMessage($"The map by key {key} does not exist!", _channel);
                    return;
                }
                _cachedBeatmaps.Add(key, beatmap);
            }

            Beatmap map = _cachedBeatmaps[key];

            float rating = map.Stats.Rating;
            float length = map.Metadata.Duration / 60f;

            if (rating < _config.Onyx.MinimumRating)
            {
                _service.SendTextMessage($"The map's rating of {string.Format("{0:0.##}", rating * 100)}% is too low! It needs at least a {string.Format("{0:0.##}", _config.Onyx.MinimumRating * 100)}%.", _channel);
                return;
            }

            if (length > _config.Onyx.MaximumSongLength)
            {
                _service.SendTextMessage($"Song is too long! Maximum song length is {_config.Onyx.MaximumSongLength} minutes.", _channel);
                return;
            }

            if (length < _config.Onyx.MinimumSongLength)
            {
                _service.SendTextMessage($"Song is too short! Song must be at least {_config.Onyx.MinimumSongLength} minutes long.", _channel);
                return;
            }

            // Find the highest and lowest NJS in the level.
            float highestNJS = 0;
            float lowestNJS = 0;
            foreach (var chr in map.Metadata.Characteristics)
            {
                foreach (var diff in chr.Difficulties.Values)
                {
                    if (diff.HasValue)
                    {
                        float njs = diff.Value.NoteJumpSpeed;
                        if (njs > highestNJS)
                            highestNJS = njs;
                        if (njs < lowestNJS)
                            lowestNJS = njs;
                    }
                }
            }

            if (lowestNJS > _config.Onyx.MaximumNJS)
            {
                _service.SendTextMessage($"This song is too fast! Maximum NJS is {_config.Onyx.MaximumNJS}.", _channel);
                return;
            }

            if (highestNJS < _config.Onyx.MinimumNJS)
            {
                _service.SendTextMessage($"This song is too slow! Minimum NJS is {_config.Onyx.MinimumNJS}.", _channel);
                return;
            }

            

            // Load the cover for later use.
            await Cacher.LoadCover(key);

            var request = new OnyxRequest()
            {
                key = key,
                requestor = requestor,
                requesterName = username,
                beatmap = map
            };

            if (forceToTop)
            {
                _requestQueue.Insert(0, request);
            }
            else
            {
                _requestQueue.Add(request);
            }


            _config.Onyx.RequestHistory.Add(new RequestData { key = key, requestDate = DateTime.Now.ToString() });
            _service.SendTextMessage($"Added \"{map.Name}\" by {map.Uploader.Username} ({key}) to the queue.", _channel);

            Plugin.Log.Notice("Songs in queue: " + _requestQueue.Count);
        }

        public void OnDestroy()
        {
            Plugin.TwitchMessageReceived -= TwitchMessageReceived;
        }
    }
}
