using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Unify.Patches;

namespace Unify
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    public class UnifyPlugin : BasePlugin
    {
        public const string Id = "daemon.unify";
        private const string Name = "Unify";
        private const string Version = "3.0.0-pre.1";

        public static ConfigFile ConfigFile;

        // public static readonly bool HandshakeDisabled = !PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value;
        
        /*public static readonly string[] NormalHandshake =
            new string[] {"North America", "Europe", "Asia", "skeld.net"};*/

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            // ===== For compatibility reasons =====
            string oldConfigPath = Path.Combine(Paths.ConfigPath, "daemon.unify.reactor.cfg");
            string newConfigPath = Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg");
            bool oldConfigExists = File.Exists(oldConfigPath);
            if (oldConfigExists)
            {
                File.Copy(oldConfigPath, newConfigPath, true);
                File.Delete(oldConfigPath);
            }

            ConfigFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg"), true);
            // =====================================
            
            RegionsPatch.Patch();

            Harmony.PatchAll();
        }

        public static IRegionInfo[] MergeRegions(IRegionInfo[] oldRegions, IRegionInfo[] newRegions)
        {
            IRegionInfo[] patchedRegions = new IRegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            return patchedRegions;
        }

        public static IRegionInfo AddRegion(string name, string ip)
        {
            if (Uri.CheckHostName(ip) != UriHostNameType.IPv4)
                return DestroyableSingleton<ServerManager>.CMJOLNCMAPD.KIDDJMGEOKK;

            IRegionInfo existingRegion =
                ServerManager.DefaultRegions.ToArray().FirstOrDefault(region => region.PingServer == ip);
            if (existingRegion != null) return existingRegion;
            
            IRegionInfo newRegion = new DnsRegionInfo(ip, name, StringNames.NoTranslation, ip, 22023)
                .Cast<IRegionInfo>();
            
            RegionsPatch.ModRegions.Add(newRegion);
            RegionsPatch.Patch();

            return newRegion;
        }

        public static bool SetDirectRegion(string ip, out IRegionInfo newRegion)
        {
            newRegion = null;
            
            if (!Regex.IsMatch(ip, @"^(\d{1,3}\.){3}\d{1,3}$")) return false;
            
            newRegion = new DnsRegionInfo(ip, ip, StringNames.NoTranslation, ip, 22023)
                .Cast<IRegionInfo>();

            RegionsPatch.DirectRegion = newRegion;
            RegionsPatch.Patch();

            return true;
        }
    }
}