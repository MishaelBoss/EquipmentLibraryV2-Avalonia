using System;
using System.IO;

namespace EquipmentLibraryV2_Avalonia.Scripts 
{
    public abstract class AppPaths
    {
        private static string LauncherDir => AppDomain.CurrentDomain.BaseDirectory;

        public static string UserDataDir
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppConfig.Applicationnames);

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                return path;
            }
        }

        public static string DefaultCacheDir => Path.Combine(LauncherDir, "cache");
        public static string DefaultUpdateDir => Path.Combine(LauncherDir, "updates");
        public static string DefaultDownloadDir => Path.Combine(LauncherDir, "downloads");
    }
}