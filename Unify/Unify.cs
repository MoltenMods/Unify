using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
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
        public const string Id = "daemon.unify.reactor";
        private const string Name = "Unify";
        private const string Version = "2.0.3";
        
        public static readonly ConfigFile ConfigFile =
            new ConfigFile(Path.Combine(Paths.ConfigPath, $"{UnifyPlugin.Id}.cfg"), true);

        public static readonly bool HandshakeDisabled = !PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value;
        
        public static readonly string[] NormalHandshake =
            new string[] {"North America", "Europe", "Asia", "skeld.net"};

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
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

        public static void AddRegion(string name, string ip)
        {
            IRegionInfo newRegion = new DnsRegionInfo(ip, name, StringNames.NoTranslation, ip)
                .Cast<IRegionInfo>();
            
            RegionsPatch.ModRegions.Add(newRegion);
            RegionsPatch.Patch();
        }
    }
}