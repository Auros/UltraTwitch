using System;

namespace UltraTwitch.OnyxRequest
{
    public class RequestData
    {
        public string key;
        public string requestDate;

        public DateTime RequestDate()
        {
            return DateTime.Parse(requestDate);
        }
    }
}
