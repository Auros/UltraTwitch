using System;

namespace UltraTwitch
{
    public class UltraTwitchUser
    {
        public string ID { get; set; }
        public string LastSeen { get; set; } = DateTime.Now.ToString();
        public bool CanRequestSongs { get; set; } = true;
        public bool CanPostImages { get; set; } = true;
        public bool FullOverride { get; set; } = false;

        public DateTime LastSeenDate()
        {
            return DateTime.Parse(LastSeen);
        }
    }
}
