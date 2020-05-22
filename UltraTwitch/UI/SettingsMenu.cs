using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;

namespace UltraTwitch.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        [UIValue("current-channel")]
        public string CurrentChannel = Plugin.Config.MainChannel;


        [UIValue("active-channel")]
        public List<object> AllChannels => Plugin.TwitchService.Channels.Keys.Select(c => c as object).Reverse().ToList();

        [UIAction("#apply")]
        public void Apply()
        {
            Plugin.Config.MainChannel = CurrentChannel;

            Plugin.CheckActiveChannel();
        }
    }
}
