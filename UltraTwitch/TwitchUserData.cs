using Newtonsoft.Json;

namespace UltraTwitch
{
    public struct TwitchUserData
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("login")]
        public string Login { get; set; }
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("broadcaster_type")]
        public string BroadcasterType { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("profile_image_url")]
        public string ProfileImageURL { get; set; }
        [JsonProperty("offline_image_url")]
        public string OfflineImageURL { get; set; }
        [JsonProperty("view_count")]
        public int ViewCount { get; set; }
    }
}
