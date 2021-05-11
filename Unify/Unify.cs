using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Hazel.Udp;
using Reactor;
using Unify.Controls;
using Unify.Patches;

namespace Unify
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    [ReactorPluginSide(PluginSide.ClientOnly)]
    public class UnifyPlugin : BasePlugin
    {
        public const string Id = "daemon.unify";
        private const string Name = "Unify";
        private const string Version = "5.0.0";

        public static ConfigFile ConfigFile { get; private set; }

        internal static ConfigEntry<bool> ShowOfficialRegions;
        internal static ConfigEntry<bool> ShowExtraRegions;

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            FixCompatibility();
            
            ShowOfficialRegions = ConfigFile.Bind("Preferences", "Show Official Regions", true, 
                "If the official regions should be shown when displaying the regions menu");
            ShowExtraRegions = ConfigFile.Bind("Preferences", "Show Extra Regions", true,
                "If the extra regions added by default in Unify should be shown when displaying the regions menu");

            // Unpatches the modded handshake, because Impostor is STILL not fully updated yet
            Harmony.Unpatch(typeof(UdpConnection).GetMethod("HandleSend"), HarmonyPatchType.Prefix, 
                ReactorPlugin.Id);

            Harmony.PatchAll();
            
            Button.InitializeBaseButton();
            RegionsEditor.SetUp();
        }

        public static IRegionInfo AddRegion(string name, string ip, ushort port)
        {
            if (Uri.CheckHostName(ip) != UriHostNameType.IPv4) return ServerManager.Instance.CurrentRegion;

            IRegionInfo existingRegion =
                ServerManager.DefaultRegions.ToArray().FirstOrDefault(region => region.PingServer == ip);
            if (existingRegion != null) return existingRegion;
            
            IRegionInfo newRegion = new DnsRegionInfo(ip, name, StringNames.NoTranslation, ip, port)
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

        private static void FixCompatibility()
        {
            string oldConfigPath = Path.Combine(Paths.ConfigPath, "daemon.unify.reactor.cfg");
            string newConfigPath = Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg");
            bool oldConfigExists = File.Exists(oldConfigPath);
            if (oldConfigExists)
            {
                File.Copy(oldConfigPath, newConfigPath, true);
                File.Delete(oldConfigPath);
            }

            ConfigFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg"), true);
        }
    }
}