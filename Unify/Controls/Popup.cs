using System;
using System.Threading.Tasks;
using Reactor.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unify.Controls
{
    public class Popup
    {
        private static readonly TaskCompletionSource<bool> Initialized = new TaskCompletionSource<bool>();
        private static GameObject _instance;
        
        public GameObject GameObject { get; }
        public TextMeshPro Text { get; }

        private Popup(string text)
        {
            GameObject = Object.Instantiate(_instance).DontDestroy();

            Text = GameObject.GetComponentInChildren<TextMeshPro>();

            Text.text = text;
        }

        public static async Task<Popup> Create(string text)
        {
            await Initialized.Task;

            return new Popup(text);
        }

        public static async void Create(string text, Action<Popup> callback)
        {
            var newPopup = await Create(text);
            callback(newPopup);
        }

        public static void InitializeBasePopup()
        {
            BasePopup.Initialize();
        }

        private static class BasePopup
        {
            public static void Initialize()
            {
                SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) OnSceneLoaded);
            }

            private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
            {
                Instantiate();
                
                SceneManager.remove_sceneLoaded((Action<Scene, LoadSceneMode>) OnSceneLoaded);
            }
            
            private static void Instantiate()
            {
                if (Initialized.Task.IsCompleted) return;
                
                var disconnectPopup = GameObject.Find("DisconnectPopup");
                if (!disconnectPopup) return;

                _instance = Object.Instantiate(disconnectPopup).DontDestroy();
                _instance.SetActive(false);
                _instance.name = "Popup";
                
                _instance.GetComponentInChildren<TextMeshPro>().text = "Popup";

                Initialized.SetResult(true);
            }
        }
    }
}