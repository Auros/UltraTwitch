using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System.Collections.Generic;
using UltraTwitch.OnyxRequest;

namespace UltraTwitch
{
    public class Config
    {
        public virtual string MainChannel { get; set; }

        [UseConverter(typeof(DictionaryConverter<UltraTwitchUser>))]
        public virtual Dictionary<string, UltraTwitchUser> ViewerProfiles { get; set; } = new Dictionary<string, UltraTwitchUser>();

        [NonNullable]
        public virtual OnyxSettings Onyx { get; set; } = new OnyxSettings();
    }
}
