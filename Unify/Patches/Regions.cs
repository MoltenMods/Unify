using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel.Udp;
using Reactor;
using Array = System.Array;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        private static IRegionInfo[] _oldRegions = ServerManager.DefaultRegions;
        private static IRegionInfo[] _newRegions = new IRegionInfo[]
        {
            new DnsRegionInfo("192.241.154.115", "skeld.net", StringNames.NoTranslation, "192.241.154.115")
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("localhost", "localhost", StringNames.NoTranslation, "127.0.0.1")
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("152.228.160.91", "matux.fr", StringNames.NoTranslation, "152.228.160.91")
                .Cast<IRegionInfo>()
        };

        public static List<IRegionInfo> ModRegions = new List<IRegionInfo>();

        public static void Patch()
        {
            IRegionInfo[] customRegions = UnifyPlugin.MergeRegions(_newRegions, ModRegions.ToArray());
            customRegions = UnifyPlugin.MergeRegions(customRegions, LoadCustomUserRegions());
            IRegionInfo[] patchedRegions = UnifyPlugin.MergeRegions(_oldRegions, customRegions);

            ServerManager.DefaultRegions = patchedRegions;
            ServerManager.Instance.AvailableRegions = patchedRegions;
            ServerManager.Instance.SaveServers();
        }

        private static IRegionInfo[] LoadCustomUserRegions()
        {
            List<IRegionInfo> customRegions = new List<IRegionInfo>();
            
            for (int x = 0; x < 5; x++)
            {
                ConfigEntry<string> regionName = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", $"Name", "custom region");
                ConfigEntry<string> regionIp = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", "IP", "");

                if (String.IsNullOrWhiteSpace(regionIp.Value)) continue;

                IRegionInfo regionInfo = new DnsRegionInfo(
                    regionIp.Value, regionName.Value, StringNames.NoTranslation, regionIp.Value)
                    .Cast<IRegionInfo>();
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
    }

    [HarmonyPatch(typeof(UdpConnection), nameof(UdpConnection.HandleSend))]
    public static class DisableModdedHandshakePatch
    {
        [HarmonyBefore(new string[] { "gg.reactor.api" })]
        public static void Prefix()
        {
            if (UnifyPlugin.HandshakeDisabled) return;
            if (!UnifyPlugin.NormalHandshake.Contains(ServerManager.Instance.CurrentRegion.Name)) return;
            
            PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value = false;
        }

        public static void Postfix()
        {
            if (UnifyPlugin.HandshakeDisabled) return;
            
            PluginSingleton<ReactorPlugin>.Instance.ModdedHandshake.Value = true;
        }
    }
}