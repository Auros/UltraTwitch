using BeatSaverSharp;

namespace UltraTwitch.OnyxRequest
{
    public struct OnyxRequest
    {
        public string key;
        public UltraTwitchUser requestor;
        public string requesterName;
        public Beatmap beatmap;
    }
}
