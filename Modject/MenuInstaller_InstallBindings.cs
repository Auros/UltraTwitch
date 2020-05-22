using HarmonyLib;
using IPA.Utilities;
using System;
using Zenject;

namespace Modject
{
    [HarmonyPatch(typeof(MenuInstaller), "InstallBindings")]
    internal class MenuInstaller_InstallBindings
    {
        private static readonly PropertyAccessor<MonoInstallerBase, DiContainer>.Getter AccessDiContainer = PropertyAccessor<MonoInstallerBase, DiContainer>.GetGetter("Container");
        private static readonly PropertyAccessor<MonoInstallerBase, DiContainer>.Setter SetDiContainer = PropertyAccessor<MonoInstallerBase, DiContainer>.GetSetter("Container");

        internal static void Postfix(ref MenuInstaller __instance)
        {
            // Convert the main installer to a MonoInstaller base 
            MonoInstallerBase mainInstallerAsMono = __instance as MonoInstallerBase;

            // Inject the mono installers
            foreach (Type t in Injector._menuMonoInstallers)
            {
                // Create the mono installer's game object.
                MonoInstallerBase injectingInstallerBase = __instance.gameObject.AddComponent(t) as MonoInstallerBase;

                // Replace the container from the mod with the one from the menu installer.
                SetDiContainer(ref injectingInstallerBase, AccessDiContainer(ref mainInstallerAsMono));

                // Force install their bindings with the menu's DiContainer
                injectingInstallerBase.InstallBindings();
            }
        }
    }
}
