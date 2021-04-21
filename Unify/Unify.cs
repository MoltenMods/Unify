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
        private const string Version = "4.1.0";

        public static ConfigFile ConfigFile { get; private set; }

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

            // Unpatches the modded handshake, because Impostor is STILL not fully updated yet
            Harmony.Unpatch(typeof(UdpConnection).GetMethod("HandleSend"), HarmonyPatchType.Prefix, 
                ReactorPlugin.Id);

            Harmony.PatchAll();
        }

        public static IRegionInfo AddRegion(string name, string ip, ushort port)
        {
            if (Uri.CheckHostName(ip) != UriHostNameType.IPv4) return ServerManager.Instance.CurrentRegion;

            IRegionInfo existingRegion =
                ServerManager.DefaultRegions.ToArray().FirstOrDefault(region => region.PingServer == ip);
            if (existingRegion != null) return existingRegion;
            
            IRegionInfo newRegion = new DnsRegionInfo(ip, $"{name}\n{ip}   {port}", StringNames.NoTranslation, ip, port)
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