using System;
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
            
            RegionInfo[] oldRegions = ServerManager.DefaultRegions;
            RegionInfo[] patchedRegions = new RegionInfo[oldRegions.Length + newRegions.Length];
            Array.Copy(oldRegions, patchedRegions, oldRegions.Length);
            Array.Copy(newRegions, 0, patchedRegions, oldRegions.Length, newRegions.Length);

            ServerManager.DefaultRegions = patchedRegions;
        }
    }
}