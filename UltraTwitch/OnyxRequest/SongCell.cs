using BeatSaverSharp;
using BeatSaberMarkupLanguage.Components;

namespace UltraTwitch.OnyxRequest
{
    public class SongCell : CustomListTableData.CustomCellInfo
    {
        public SongCell(Beatmap map, OnyxRequest req) : base("", "", null)
        {
            text = map.Name;

            subtext = $"Requested by {req.requesterName}";

            icon = Cacher.LoadCachedCover(req.key);
        }
    }
}
