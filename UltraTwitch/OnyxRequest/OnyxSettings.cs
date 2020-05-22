using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UltraTwitch.OnyxRequest
{
    public class OnyxSettings
    {
        public virtual bool Enabled { get; set; } = false;
        public virtual bool On { get; set; } = true;
        public virtual string RequestPrefix { get; set; } = "!bsr";
        public virtual int NormalUsersMaxRequests { get; set; } = 2;
        public virtual int SubscribersMaxRequests { get; set; } = 5;
        public virtual int VIPMaxRequests { get; set; } = 5;
        public virtual int ModMaxRequests { get; set; } = -1;
        public virtual bool AllowMultipleInSession { get; set; } = false;
        public virtual float MinimumNJS { get; set; } = 0;
        public virtual float MaximumNJS { get; set; } = 30;
        public virtual float MinimumSongLength { get; set; } = 0;
        public virtual float MaximumSongLength { get; set; } = 20;
        public virtual float MinimumRating { get; set; } = 0f;
        public virtual float SessionLength { get; set; } = 6f;

        [UseConverter(typeof(ListConverter<RequestData>)), NonNullable]
        public virtual List<RequestData> RequestHistory { get; set; } = new List<RequestData>();

        [UseConverter(typeof(ListConverter<string>)), NonNullable]
        public virtual List<string> BannedRequests { get; set; } = new List<string>();
    }
}
