using System;
using System.Threading.Tasks;
using Reactor.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unify.Controls
{
    public class Button
    {
        private static readonly TaskCompletionSource<bool> Initialized = new TaskCompletionSource<bool>();
        private static GameObject _instance;
        
        private readonly SpriteRenderer _spriteRenderer;
        private readonly BoxCollider2D _boxCollider2D;

        public GameObject GameObject { get; }
        public TextMeshPro Text { get; }
        public Vector2 Size
        {
            get => _spriteRenderer.size;
            set
            {
                Text.rectTransform.sizeDelta = value;
                _spriteRenderer.size = value;
                _boxCollider2D.size = value;
            }
        }
        public AspectPosition Position { get; }
        public UnityEngine.UI.Button.ButtonClickedEvent OnClick { get; }

        private Button(string text)
        {
            GameObject = Object.Instantiate(_instance).DontDestroy();

            Text = GameObject.GetComponentInChildren<TextMeshPro>();
            Position = GameObject.GetComponent<AspectPosition>();
            OnClick = GameObject.GetComponent<PassiveButton>().OnClick;
            _spriteRenderer = GameObject.GetComponent<SpriteRenderer>();
            _boxCollider2D = GameObject.GetComponent<BoxCollider2D>();
            
            var size = new Vector2(Text.preferredWidth, Text.preferredHeight);

            Text.text = text;
            Text.rectTransform.sizeDelta = size;
            Position.DistanceFromEdge =
                new Vector3(Text.preferredWidth / 2f + 0.1f, Text.preferredHeight / 2f + 0.1f, -100f);
            _spriteRenderer.size = size;
            _boxCollider2D.size = size;
        }

        public static async Task<Button> Create(string text)
        {
            await Initialized.Task;

            return new Button(text);
        }

        public Button SetText(string newText)
        {
            Text.text = newText;
            return this;
        }

        public Button SetSize(Vector2 newSize)
        {
            Size = newSize;
            return this;
        }

        public Button SetDistanceFromEdge(Vector2 newPosition)
        {
            Position.DistanceFromEdge = new Vector3(newPosition.x, newPosition.y, -100f);
            return this;
        }

        public Button SetAlignment(AspectPosition.EdgeAlignments newAlignment)
        {
            Position.Alignment = newAlignment;
            return this;
        }

        public static void InitializeBaseButton()
        {
            BaseButton.Initialize();
        }

        private static class BaseButton
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
                
                var exitButton = GameObject.Find("ExitGameButton");
                if (!exitButton) return;

                _instance = Object.Instantiate(exitButton).DontDestroy();
                _instance.SetActive(false);
                _instance.name = "Button";

                _instance.GetComponent<PassiveButton>().OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                _instance.GetComponent<SceneChanger>().Destroy();
                _instance.GetComponent<ConditionalHide>().Destroy();
                _instance.GetComponentInChildren<TextTranslatorTMP>().Destroy();
                _instance.GetComponentInChildren<TextMeshPro>().text = "Button";

                Initialized.SetResult(true);
            }
        }
    }
}