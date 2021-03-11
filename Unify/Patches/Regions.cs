using System.Collections.Generic;
using BepInEx.Configuration;
using Array = System.Array;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        private static IRegionInfo[] _newRegions = new IRegionInfo[]
        {
            new StaticRegionInfo("skeld.net", StringNames.NoTranslation, "192.241.154.115", new ServerInfo[]
            {
                new ServerInfo("skeld.net-Master-1", "192.241.154.115", 22023)
            }).Duplicate(),
            new StaticRegionInfo("localhost", StringNames.NoTranslation, "127.0.0.1", new ServerInfo[]
            {
                new ServerInfo("localhost", "127.0.0.1", 22023)
            }).Duplicate()
        };
        private static IRegionInfo[] _customRegions = MergeRegions(_newRegions, LoadCustomRegions());
        
        public static void Patch()
        {
            IRegionInfo[] patchedRegions = MergeRegions(ServerManager.DefaultRegions, _customRegions);
            
            ServerManager.DefaultRegions = patchedRegions;
            ServerManager.Instance.AvailableRegions = patchedRegions;
            ServerManager.Instance.SaveServers();
        }

        private static IRegionInfo[] MergeRegions(IRegionInfo[] oldRegions, IRegionInfo[] newRegions)
        {
            IRegionInfo[] patchedRegions = new IRegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            return patchedRegions;
        }
        
        private static IRegionInfo[] LoadCustomRegions()
        {
            List<IRegionInfo> customRegions = new List<IRegionInfo>();
            
            for (int x = 0; x < 5; x++)
            {
                ConfigEntry<string> regionName = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", $"Name", "custom region");
                ConfigEntry<string> regionIp = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", "IP", "");
                ConfigEntry<ushort> regionPort = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", "Port", (ushort) 22023);
                
                if (String.IsNullOrWhiteSpace(regionIp.Value)) continue;

                IRegionInfo regionInfo = new StaticRegionInfo(
                    regionName.Value, StringNames.NoTranslation, regionIp.Value, new ServerInfo[] 
                    {
                        new ServerInfo(regionName.Value, regionIp.Value, regionPort.Value)
                    }).Duplicate();
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
    }
}