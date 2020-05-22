using Zenject;
using ChatCore;
using ChatCore.Models.Twitch;
using ChatCore.Services.Twitch;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using UnityEngine;
using HMUI;
using System.Linq;
using BeatSaberMarkupLanguage;

namespace UltraTwitch.UI
{
    [ViewDefinition("UltraTwitch.Views.overlay.bsml")]
    [HotReload("C:\\Users\\Aurora\\Programming\\Beat Saber Mods\\UltraTwitch\\UltraTwitch\\Views\\overlay.bsml")]
    public class OverlayViewController : BSMLAutomaticViewController
    {
        private Config _config;
        private TwitchChannel _channel;
        private TwitchService _service;

        internal FloatingScreen floatingScreen;

        private bool _toggleHandle = false;
        private bool _canOpenOnyx = true;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation)
            {
                base.DidActivate(firstActivation, type);
                _config = Plugin.Config;
                _service = Plugin.TwitchService;
                _channel = _service.Channels[_config.MainChannel].AsTwitchChannel();
                // TODO: Update channel variable when the config changes. 
            }
        }


        [UIAction("toggled-handle")]
        public void ToggleHandle()
            => _toggleHandle = floatingScreen.ShowHandle = !_toggleHandle;

        [UIAction("chatted")]
        public void Chatted(string chatMessage)
        {
            if (!string.IsNullOrWhiteSpace(chatMessage))
                _service.SendTextMessage(chatMessage, _channel);
        }

        
        [UIAction("toggle-onyx-requests")]
        public void ToggleOnyxRequestPage()
        {
            if (_canOpenOnyx)
            {
                var currentFlow = Resources.FindObjectsOfTypeAll<FlowCoordinator>().FirstOrDefault(x => x.isActivated);
                if (UltraTwitchFlowCoordinator.owner == null)
                    UltraTwitchFlowCoordinator.owner = BeatSaberUI.CreateFlowCoordinator<UltraTwitchFlowCoordinator>();
                UltraTwitchFlowCoordinator.owner.oldCoordinator = currentFlow;
                UltraTwitchFlowCoordinator.owner.overlayView = this;
                currentFlow.PresentFlowCoordinator(UltraTwitchFlowCoordinator.owner, null, true);
            }
            else
            {
                UltraTwitchFlowCoordinator.owner?.oldCoordinator?.DismissFlowCoordinator(UltraTwitchFlowCoordinator.owner, null, true);
            }
            _canOpenOnyx = !_canOpenOnyx;
        }
    }
}
