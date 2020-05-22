using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UltraTwitch.OnyxRequest
{
    [ViewDefinition("UltraTwitch.Views.onyx-request.bsml")]
    [HotReload("C:\\Users\\Aurora\\Programming\\Beat Saber Mods\\UltraTwitch\\UltraTwitch\\Views\\onyx-request.bsml")]
    public class OnyxRequestView : BSMLAutomaticViewController
    {
        protected OnyxRequestBot _bot;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            if (_bot == null)
                _bot = Resources.FindObjectsOfTypeAll<OnyxRequestBot>().FirstOrDefault();

            SetupRequestsList();
        }

        [UIComponent("req-list")]
        public CustomListTableData reqList;


        private void SetupRequestsList()
        {
            if (_bot == null)
                return;

            reqList.data.Clear();
            foreach (var request in _bot.RequestQueue())
            {
                reqList.data.Add(new SongCell(request.beatmap, request)); // Legacy
            }
            reqList.tableView.ReloadData();
        }
    }
}
