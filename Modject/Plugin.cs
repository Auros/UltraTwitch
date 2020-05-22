using IPA;
using HarmonyLib;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace Modject
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Harmony Harmony { get; private set; }
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; set; }

        [Init]
        public Plugin(IPALogger logger)
        {
            Instance = this;
            Log = logger;
        }

        [OnEnable]
        public void Enabled()
        {
            Harmony = new Harmony($"dev.auros.modject");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnDisable]
        public void Disabled()
        {
            Harmony?.UnpatchAll();
            Harmony = null;
        }
    }
}

