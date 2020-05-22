using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltraTwitch.OnyxRequest;

namespace UltraTwitch.UI
{
    public class UltraTwitchFlowCoordinator : FlowCoordinator
    {
        public static UltraTwitchFlowCoordinator owner;

        public FlowCoordinator oldCoordinator;
        public OverlayViewController overlayView;

        protected OnyxRequestView _onyxRequestView;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (activationType != ActivationType.AddedToHierarchy)
                return;
            title = "Onyx Requests";
            showBackButton = true;
            if (firstActivation)
            {
                _onyxRequestView = BeatSaberUI.CreateViewController<OnyxRequestView>();
            }
            ProvideInitialViewControllers(_onyxRequestView);
        }

        protected override void BackButtonWasPressed(ViewController _)
        {
            overlayView?.ToggleOnyxRequestPage();
        }
    }
}
