using ChatCore;
using System.Linq;
using UltraTwitch.OnyxRequest;
using UnityEngine;
using Zenject;

namespace UltraTwitch
{
    public class UltraTwitchMenuInstaller : MonoInstaller
    {
        private static OnyxRequestBot _bot = null;

        public override void InstallBindings()
        {
            Plugin.Log.Debug("Installing UltraTwitch Menu Bindings");

            Container.BindInstance(Plugin.Config);
            Container.BindInstance(Plugin.Client);
            Container.BindInstance(Plugin.TwitchService);
            Container.BindInstance(SongDataCore.Plugin.Songs);

            if (Resources.FindObjectsOfTypeAll<OnyxRequestBot>().Count() == 0)
                _bot = Container.InstantiateComponentOnNewGameObject<OnyxRequestBot>("Onyx Request Bot");
            Container.BindInstance(_bot);

            Plugin.Log.Debug("Installed UltraTwitch Menu Bindings");
        }
    }
}
