using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace Modject
{
    public static class Injector
    {
        internal static List<Type> _gameMonoInstallers = new List<Type>();
        internal static List<Type> _menuMonoInstallers = new List<Type>();


        public static void RegisterMonoInstaller<T>(InstallLocation location) where T : MonoInstaller
        {
            switch (location)
            {
                case InstallLocation.Game:
                    _gameMonoInstallers.Add(typeof(T));
                    break;
                case InstallLocation.Menu:
                    _menuMonoInstallers.Add(typeof(T));
                    break;
            }
        }

        public static void UnregisterMonoInstaller<T>()
        {
            _gameMonoInstallers.Remove(typeof(T));
            _menuMonoInstallers.Remove(typeof(T));
        }
    }

    public enum InstallLocation
    {
        Game,
        Menu
    }
}
