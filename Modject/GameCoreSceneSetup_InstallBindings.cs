using HarmonyLib;
using IPA.Utilities;
using System;
using Zenject;

namespace Modject
{
    [HarmonyPatch(typeof(GameCoreSceneSetup), "InstallBindings")]
    internal class GameCoreSceneSetup_InstallBindings
    {
        private static readonly PropertyAccessor<MonoInstallerBase, DiContainer>.Getter AccessDiContainer = PropertyAccessor<MonoInstallerBase, DiContainer>.GetGetter("Container");
        private static readonly PropertyAccessor<MonoInstallerBase, DiContainer>.Setter SetDiContainer = PropertyAccessor<MonoInstallerBase, DiContainer>.GetSetter("Container");

        internal static void Postfix(ref GameCoreSceneSetup __instance)
        {
            // Convert the main installer to a MonoInstaller base 
            MonoInstallerBase mainInstallerAsMono = __instance as MonoInstallerBase;

            // Inject the mono installers
            foreach (Type t in Injector._gameMonoInstallers)
            {
                // Create the mono installer's game object.
                MonoInstallerBase injectingInstallerBase = __instance.gameObject.AddComponent(t) as MonoInstallerBase;

                // Replace the container from the mod with the one from the gameplay core scene setup.
                SetDiContainer(ref injectingInstallerBase, AccessDiContainer(ref mainInstallerAsMono));

                // Force install their bindings with the gameplay setup's DiContainer
                injectingInstallerBase.InstallBindings();
            }
        }
    }
}
