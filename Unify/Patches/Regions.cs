using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace Unify.Patches
{
    [HarmonyPatch]
    public static class RegionsPatch
    {
        public static void Patch()
        {
            RegionInfo[] newRegions = new RegionInfo[]
            {
                new RegionInfo("skeld.net", "192.241.154.115", new ServerInfo[]
                {
                    new ServerInfo("skeld.net-Master-1", "192.241.154.115", 22023)
                }),
                new RegionInfo("localhost", "127.0.0.1", new ServerInfo[]
                {
                    new ServerInfo("localhost-Master-1", "127.0.0.1", 22023)
                })
            };
            
            RegionInfo[] customRegions = LoadCustomRegions();
            
            RegionInfo[] patchedRegions = MergeRegions(ServerManager.DefaultRegions, newRegions);
            patchedRegions = MergeRegions(patchedRegions, customRegions);

            ServerManager.DefaultRegions = patchedRegions;
        }

        private static RegionInfo[] MergeRegions(RegionInfo[] oldRegions, RegionInfo[] newRegions)
        {
            RegionInfo[] patchedRegions = new RegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            return patchedRegions;
        }
        
        private static RegionInfo[] LoadCustomRegions()
        {
            List<RegionInfo> customRegions = new List<RegionInfo>();
            
            for (int x = 0; x < 5; x++)
            {
                ConfigEntry<string> regionName = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", $"Name", "custom region");
                ConfigEntry<string> regionIp = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", "IP", "");
                ConfigEntry<ushort> regionPort = UnifyPlugin.ConfigFile.Bind($"Region {x + 1}", "Port", (ushort) 22023);
                
                if (String.IsNullOrWhiteSpace(regionIp.Value)) continue;

                RegionInfo regionInfo = new RegionInfo(regionName.Value, regionIp.Value, new ServerInfo[]
                {
                    new ServerInfo($"{regionName.Value}-Master-1", regionIp.Value, regionPort.Value)
                });
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
    }
}