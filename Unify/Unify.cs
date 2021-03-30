using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
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
        private const string Version = "2.1.0";
        
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

        public static IRegionInfo AddRegion(string name, string ip)
        {
            if (Uri.CheckHostName(ip) != UriHostNameType.IPv4) return ServerManager.Instance.CurrentRegion;

            IRegionInfo existingRegion =
                ServerManager.DefaultRegions.ToArray().FirstOrDefault(region => region.PingServer == ip);
            if (existingRegion != null) return existingRegion;
            
            IRegionInfo newRegion = new DnsRegionInfo(ip, name, StringNames.NoTranslation, ip)
                .Cast<IRegionInfo>();
            
            RegionsPatch.ModRegions.Add(newRegion);
            RegionsPatch.Patch();

            return newRegion;
        }

        public static IRegionInfo SetDirectRegion(string ip)
        {
            if (Uri.CheckHostName(ip) != UriHostNameType.IPv4) return ServerManager.Instance.CurrentRegion;
            
            IRegionInfo newRegion = new DnsRegionInfo(ip, ip, StringNames.NoTranslation, ip)
                .Cast<IRegionInfo>();

            RegionsPatch.DirectRegion = newRegion;
            RegionsPatch.Patch();

            return newRegion;
        }

        public static void SetRegionIp(string ip)
        {
            IRegionInfo existingRegion =
                ServerManager.DefaultRegions.ToArray().FirstOrDefault(region => region.PingServer == ip);
            if (existingRegion != null) return;
            
            RegionMenu regionMenu = DestroyableSingleton<RegionMenu>.Instance;
            IRegionInfo newRegion = AddRegion(ip, ip);

            // ChatLanguageButton lastRegionButton = regionMenu.ButtonPool.activeChildren[^1].Cast<ChatLanguageButton>();

            // regionMenu.ChooseOption(ServerManager.DefaultRegions[^1]);
            // regionMenu.ButtonPool.activeChildren[^1].Cast<ChatLanguageButton>().Button.ReceiveClickDown();
            // lastRegionButton.gameObject.SetActive(true);
            // lastRegionButton.Button.OnClick.Invoke();
            regionMenu.ChooseOption(newRegion);
        }
    }
}