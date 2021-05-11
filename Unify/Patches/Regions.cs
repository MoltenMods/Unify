using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;
using String = System.String;

namespace Unify.Patches
{
    public static class RegionsPatch
    {
        internal static IRegionInfo[] OldRegions { get; } = ServerManager.DefaultRegions;

        internal static IRegionInfo[] NewRegions { get; } = new IRegionInfo[]
        {
            new DnsRegionInfo("192.241.154.115", "skeld.net", StringNames.NoTranslation, "192.241.154.115", 22023)
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("localhost", "localhost", StringNames.NoTranslation, "127.0.0.1", 22023)
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("152.228.160.91", "matux.fr", StringNames.NoTranslation, "152.228.160.91", 22023)
                .Cast<IRegionInfo>(),
            new DnsRegionInfo("77.55.217.159", "Przebot", StringNames.NoTranslation, "77.55.217.159", 22023)
                .Cast<IRegionInfo>()
        };

        internal static List<IRegionInfo> ModRegions { get; } = new List<IRegionInfo>();

        public static IRegionInfo DirectRegion { get; set; }

        internal static TextBoxTMP DirectConnect { get; set; }

        public static void Patch()
        {
            ServerManager serverManager = ServerManager.Instance;

            IRegionInfo[] patchedRegions = OldRegions
                .AddRangeToArray(NewRegions)
                .AddRangeToArray(ModRegions.ToArray())
                .AddRangeToArray(LoadCustomUserRegions());
            if (DirectRegion != null) patchedRegions = patchedRegions.AddToArray(DirectRegion);

            ServerManager.DefaultRegions = patchedRegions;
            serverManager.AvailableRegions = patchedRegions;
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
                        regionIp.Value, regionName.Value,
                        StringNames.NoTranslation, regionIp.Value, regionPort.Value)
                    .Cast<IRegionInfo>();

                customRegions.Add(regionInfo);
            }

            return customRegions.ToArray();
        }

        private static void UpdateRegion()
        {
            RegionMenu regionMenu = DestroyableSingleton<RegionMenu>.Instance;

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
        public static class LoadNewRegionsPatch
        {
            public static void Postfix()
            {
                Patch();
            }
        }

        [HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
        public static class DirectConnectEnterButtonPatch {
            public static bool Prefix(JoinGameButton __instance)
            {
                GameObject regionMenuGameObject = GameObject.Find("RegionMenu");
                if (!regionMenuGameObject) return true;

                UpdateRegion();

                return false;
            }
        }

        [HarmonyPatch(typeof(RegionMenu))]
        public static class RegionMenuPatches
        {
            [HarmonyPatch(nameof(RegionMenu.Open))]
            [HarmonyPostfix]
            public static void DirectConnectButtonPatch()
            {
                RefreshDirectConnectButton();
            }

            [HarmonyPatch(nameof(RegionMenu.Open))]
            [HarmonyPostfix]
            public static void RegionMenuLayoutPatch()
            {
                RefreshRegionMenu();
            }

            internal static void RefreshRegionMenu()
            {
                if (!GameObject.Find("RegionMenu")) return;
                
                var regionButtons = DestroyableSingleton<RegionMenu>.Instance.ButtonPool.activeChildren.ToArray();
                var defaultRegions = ServerManager.DefaultRegions;
                var showOfficials = UnifyPlugin.ShowOfficialRegions.Value;
                var showExtras = UnifyPlugin.ShowExtraRegions.Value;

                for (int i = 0; i < regionButtons.Length; i++)
                {
                    ServerListButton regionButton = regionButtons[i].Cast<ServerListButton>();
                    IRegionInfo region = defaultRegions[i];

                    var textTranslatorTemp = regionButton.GetComponentInChildren<TextTranslatorTMP>();
                    if (textTranslatorTemp) textTranslatorTemp.Destroy();

                    int extraRegionCount = OldRegions.Length + NewRegions.Length + ModRegions.Count;
                    bool isCustomRegion = i >= OldRegions.Length;
                    bool isCustomUserRegion = i >= extraRegionCount;
                    float x = (!isCustomRegion && !showExtras ? 3f : 0) + (isCustomRegion ? 3f : 0) +
                        (isCustomUserRegion ? 3f : 0) - 4.25f - (!showOfficials || OldRegions.Length == 0 ? 1.5f : 0) -
                        (!showExtras || extraRegionCount == 0 ? 1.5f : 0);
                    float y = i - (isCustomRegion ? OldRegions.Length : 0) - 
                              (isCustomUserRegion ? NewRegions.Length + ModRegions.Count : 0);

                    var regionButtonPosition = new Vector3(1.25f + x, 2f - 0.5f * y, 0);
                    regionButton.transform.localPosition = regionButtonPosition;

                    regionButton.gameObject.SetActive(
                        (showOfficials || i >= OldRegions.Length) &&
                        (showExtras || !isCustomRegion || isCustomUserRegion));
                    
                    var buttonRolloverHandler = regionButton.GetComponent<ButtonRolloverHandler>();

                    if (RegionsEditor.IsActive)
                    {
                        buttonRolloverHandler.OverColor =
                            ServerManager.Instance.CurrentRegion.Name == region.Name ? Color.white : Color.black;
                        regionButton.Text.color = Color.gray;
                    }
                    else
                    {
                        buttonRolloverHandler.OverColor = Color.green;
                        regionButton.Text.color = Color.white;
                    }

                    if (isCustomUserRegion)
                    {
                        regionButton.Text.text = 
                            RegionsEditor.IsActive ? $"{region.Name}\n{region.PingServer}   {region.Servers[0].Port}" :
                                region.Name;
                    }
                }
            }

            internal static void RefreshDirectConnectButton()
            {
                if (!GameObject.Find("RegionMenu")) return;
                
                if (DirectConnect) DirectConnect.gameObject.Destroy();

                RegionMenu regionMenu = DestroyableSingleton<RegionMenu>.Instance;
                JoinGameButton joinGameButton = DestroyableSingleton<JoinGameButton>.Instance;

                DirectConnect = Object.Instantiate(joinGameButton.GameIdText, regionMenu.transform);
                DirectConnect.GetComponentInChildren<TextTranslatorTMP>().Destroy();
                DirectConnect.GetComponent<JoinGameButton>().Destroy();
                DirectConnect.outputText.text = "Enter IP";
                DirectConnect.IpMode = true;
                DirectConnect.characterLimit = 15;
                DirectConnect.ClearOnFocus = false;

                DirectConnect.OnEnter = new Button.ButtonClickedEvent();
                DirectConnect.OnEnter.AddListener((Action) UpdateRegion);

                int offset = NewRegions.Length + ModRegions.Count;
                DirectConnect.transform.localPosition = new Vector3(0, 1f - (offset / 2f), -100f);
            }
        }
    }
    
    internal static class RegionsEditor
    {
        private static Controls.Button _switchButton;
        
        private static Controls.Button _toggleOfficialsButton;
        private static Controls.Button _toggleExtrasButton;

        internal static bool IsActive;

        internal static async void SetUp()
        {
            await SetUpSwitchButton();
            await SetUpToggleOfficialsButton();
            await SetUpToggleExtrasButton();
        }

        private static void SetRegionsEditorVisibility(bool visible)
        {
            IsActive = visible;
            _toggleOfficialsButton.GameObject.SetActive(visible);
            _toggleExtrasButton.GameObject.SetActive(visible);
            
            RegionsPatch.RegionMenuPatches.RefreshRegionMenu();

            if (visible)
            {
                _switchButton.Text.text = "Back";
                RegionsPatch.DirectConnect.gameObject.SetActive(false);
            }
            else
            {
                _switchButton.Text.text = "Edit";
                RegionsPatch.RegionMenuPatches.RefreshDirectConnectButton();
            }
        }

        private static async Task SetUpSwitchButton()
        {
            _switchButton = await Controls.Button.Create("Edit");
            _switchButton.SetSize(new Vector2(0.8f, 0.4f)).SetDistanceFromEdge(new Vector2(0.5f, 0.3f));
                
            _switchButton.OnClick.AddListener((Action) (() =>
            {
                SetRegionsEditorVisibility(!IsActive);
            }));
        }

        private static async Task SetUpToggleOfficialsButton()
        {
            _toggleOfficialsButton = await Controls.Button.Create("Toggle Official Regions");
            _toggleOfficialsButton.SetSize(new Vector2(2f, 0.4f)).SetDistanceFromEdge(new Vector2(1.1f, 1.3f));
                
            _toggleOfficialsButton.OnClick.AddListener((Action) (() =>
            {
                UnifyPlugin.ShowOfficialRegions.Value = !UnifyPlugin.ShowOfficialRegions.Value;
                RegionsPatch.RegionMenuPatches.RefreshRegionMenu();
            }));
        }

        private static async Task SetUpToggleExtrasButton()
        {
            _toggleExtrasButton = await Controls.Button.Create("Toggle Extra Regions");
            _toggleExtrasButton.SetSize(new Vector2(2f, 0.4f)).SetDistanceFromEdge(new Vector2(1.1f, 1.8f));
                
            _toggleExtrasButton.OnClick.AddListener((Action) (() =>
            {
                UnifyPlugin.ShowExtraRegions.Value = !UnifyPlugin.ShowExtraRegions.Value;
                RegionsPatch.RegionMenuPatches.RefreshRegionMenu();
            }));
        }

        [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
        public static class OpenRegionsEditorPatch
        {
            public static void Postfix()
            {
                if (!GameObject.Find("RegionMenu")) return;
                
                _switchButton.GameObject.SetActive(true);
            }
        }
        
        [HarmonyPatch(typeof(RegionMenu))]
        public static class HideRegionsEditorPatch
        {
            [HarmonyPatch(nameof(RegionMenu.Close))]
            [HarmonyPostfix]
            public static void HideOnClosePatch()
            {
                Hide();
            }
            
            [HarmonyPatch(nameof(RegionMenu.ChooseOption))]
            [HarmonyPrefix]
            public static bool DisableRegionButtonsPatch()
            {
                if (IsActive) return false;
                
                Hide();
                return true;
            }

            private static void Hide()
            {
                if (!RegionsPatch.DirectConnect) return;
                
                SetRegionsEditorVisibility(false);
                
                _switchButton.GameObject.SetActive(false);
                RegionsPatch.DirectConnect.gameObject.SetActive(false);
            }
        }
    }
}

// TODO:
//  Store custom servers in a JSON file