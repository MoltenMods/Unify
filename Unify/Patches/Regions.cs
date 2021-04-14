using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        private static IRegionInfo[] _oldRegions { get; } = ServerManager.DefaultRegions;
        
        private static IRegionInfo[] _newRegions { get; } = new IRegionInfo[]
        {
            new DnsRegionInfo("192.241.154.115", "skeld.net", StringNames.NoTranslation, "192.241.154.115", 22023)
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("localhost", "localhost", StringNames.NoTranslation, "127.0.0.1", 22023)
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("152.228.160.91", "matux.fr", StringNames.NoTranslation, "152.228.160.91", 22023)
                .Cast<IRegionInfo>()
        };

        public static List<IRegionInfo> ModRegions { get; } = new List<IRegionInfo>();

        public static IRegionInfo DirectRegion { get; set; }

        private static TextBoxTMP DirectConnect { get; set; }

        public static void Patch()
        {
            ServerManager serverManager = ServerManager.Instance;
            
            IRegionInfo[] patchedRegions = _oldRegions
                .AddRangeToArray(_newRegions)
                .AddRangeToArray(ModRegions.ToArray())
                .AddRangeToArray(LoadCustomUserRegions());
            if (DirectRegion != null) patchedRegions = patchedRegions.AddToArray(DirectRegion);

            ServerManager.DefaultRegions = patchedRegions;
            serverManager.AvailableRegions = patchedRegions;
            serverManager.SaveServers();
        }

        private static IRegionInfo[] LoadCustomUserRegions()
        {
            List<IRegionInfo> customRegions = new List<IRegionInfo>();
            
            for (int x = 0; x < 9; x++)
            {
                ConfigEntry<string> regionName = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", $"Name", "custom region");
                ConfigEntry<string> regionIp = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", "IP", "");
                ConfigEntry<ushort> regionPort = UnifyPlugin.ConfigFile.Bind(
                    $"Region {x + 1}", "Port", (ushort) 22023);

                if (String.IsNullOrWhiteSpace(regionIp.Value)) continue;

                IRegionInfo regionInfo = new DnsRegionInfo(
                    regionIp.Value, regionName.Value, StringNames.NoTranslation, regionIp.Value, regionPort.Value)
                    .Cast<IRegionInfo>();
                
                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }
        
        private static void UpdateRegion()
        {
            RegionMenu regionMenu = GameObject.Find("RegionMenu").GetComponent<RegionMenu>();
            
            bool success = UnifyPlugin.SetDirectRegion(DirectConnect.text, out IRegionInfo newRegion);

            if (!success)
            {
                DirectConnect.StartCoroutine(Effects.SwayX(DirectConnect.transform, 0.75f, 0.25f));
                return;
            }
            
            regionMenu.ChooseOption(newRegion);
            regionMenu.Close();
        }

        [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.LoadServers))]
        public static class PatchRegionMenuPatch
        {
            public static void Postfix()
            {
                Patch();
            }
        }
        
        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
        public static class DirectConnectButtonPatch
        {
            public static void Postfix()
            {
                JoinGameButton joinGameButton = DestroyableSingleton<JoinGameButton>.Instance;
                RegionMenu regionMenu = DestroyableSingleton<RegionMenu>.Instance;

                DirectConnect = Object.Instantiate(joinGameButton.GameIdText, regionMenu.transform);
                DirectConnect.gameObject.SetActive(false);
                DirectConnect.IpMode = true;
                DirectConnect.characterLimit = 15;
                DirectConnect.ClearOnFocus = false;

                DirectConnect.OnEnter = new Button.ButtonClickedEvent();
                DirectConnect.OnEnter.AddListener((Action) UpdateRegion);

                int offset = _newRegions.Length + ModRegions.Count;
                DirectConnect.transform.localPosition = new Vector3(0, 1f - (offset / 2f), -100f);
            }
        }

        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
        public static class RegionMenuLayoutPatch
        {
            public static void Postfix(RegionMenu __instance)
            {
                DirectConnect.gameObject.SetActive(true);
                
                var regionButtons = __instance.ButtonPool.activeChildren.ToArray();

                for (int i = 0; i < regionButtons.Length; i++)
                {
                    ServerListButton regionButton = regionButtons[i].Cast<ServerListButton>();

                    (float x, float y) = CalculatePosition(i);
                    regionButton.transform.localPosition = new Vector3(1.25f + x, 2f - 0.5f * y, 0f);
                }
            }

            private static (float x, float y) CalculatePosition(int index)
            {
                // currently broken
                /*if (DirectRegion != null && ServerManager.DefaultRegions[index].Name == DirectRegion.Name)
                {
                    int offset = Math.Max(
                        Math.Max(LoadCustomUserRegions().Length, _newRegions.Length + ModRegions.Count),
                        _oldRegions.Length);
                    return (1.25f, 2f - offset);
                }*/

                bool isCustomRegion = index >= _oldRegions.Length;
                bool isCustomUserRegion = index >= _oldRegions.Length + _newRegions.Length + ModRegions.Count;
                float x = (isCustomRegion ? 3f : 0) + (isCustomUserRegion ? 3f : 0) - 4.25f;
                float y = index - (isCustomRegion ? _oldRegions.Length : 0) -
                          (isCustomUserRegion ? _newRegions.Length + ModRegions.Count : 0);

                return (x, y);
            }
        }

        [HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
        public static class DirectConnectEnterButtonPatch
        {
            public static bool Prefix(JoinGameButton __instance)
            {
                GameObject regionMenuGameObject = GameObject.Find("RegionMenu");
                if (!regionMenuGameObject) return true;

                UpdateRegion();

                return false;
            }
        }

        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Close))]
        public static class HideDirectConnectPatch
        {
            public static void Postfix()
            {
                DirectConnect.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.ChooseOption))]
        public static class HideDirectConnectOnSelectPatch
        {
            public static void Postfix()
            {
                DirectConnect.gameObject.SetActive(false);
            }
        }
    }
}

// TODO:
//  Store custom servers in a JSON file