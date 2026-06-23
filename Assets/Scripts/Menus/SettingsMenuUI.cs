using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoDeCartas.UI
{
    public static class GameSettings
    {
        const string Prefix = "Settings_";
        const string WidthKey = Prefix + "Width";
        const string HeightKey = Prefix + "Height";
        const string FullscreenKey = Prefix + "Fullscreen";
        const string QualityKey = Prefix + "Quality";
        const string VSyncKey = Prefix + "VSync";
        const string FrameRateKey = Prefix + "FrameRate";
        const string VolumeKey = Prefix + "MasterVolume";
        public const int MinWidth = 640;
        public const int MinHeight = 360;

        static int DefaultWidth =>
            Screen.currentResolution.width > 0
                ? Screen.currentResolution.width
                : 1920;
        static int DefaultHeight =>
            Screen.currentResolution.height > 0
                ? Screen.currentResolution.height
                : 1080;
        static int DefaultQuality =>
            Mathf.Max(0, QualitySettings.names.Length - 1);

        public static int Width => PlayerPrefs.GetInt(WidthKey, DefaultWidth);
        public static int Height => PlayerPrefs.GetInt(HeightKey, DefaultHeight);
        public static bool Fullscreen =>
            PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        public static int Quality =>
            Mathf.Clamp(
                PlayerPrefs.GetInt(QualityKey, DefaultQuality),
                0,
                Mathf.Max(0, QualitySettings.names.Length - 1)
            );
        public static bool VSync =>
            PlayerPrefs.GetInt(VSyncKey, 1) == 1;
        public static int FrameRate => PlayerPrefs.GetInt(FrameRateKey, 60);
        public static float MasterVolume =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyAtStartup()
        {
            ApplySaved();
        }

        public static void SaveResolution(int width, int height)
        {
            SaveDisplay(width, height, Fullscreen);
        }

        public static void SaveDisplay(int width, int height, bool fullscreen)
        {
            PlayerPrefs.SetInt(WidthKey, Mathf.Max(MinWidth, width));
            PlayerPrefs.SetInt(HeightKey, Mathf.Max(MinHeight, height));
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyResolution();
        }

        public static void SaveFullscreen(bool value)
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyResolution();
        }

        public static void SaveQuality(int value)
        {
            PlayerPrefs.SetInt(QualityKey, value);
            PlayerPrefs.Save();
            QualitySettings.SetQualityLevel(Quality, true);
        }

        public static void SaveVSync(bool value)
        {
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPerformance();
        }

        public static void SaveFrameRate(int value)
        {
            PlayerPrefs.SetInt(FrameRateKey, value);
            PlayerPrefs.Save();
            ApplyPerformance();
        }

        public static void SaveMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
        }

        public static void ResetDefaults()
        {
            PlayerPrefs.DeleteKey(WidthKey);
            PlayerPrefs.DeleteKey(HeightKey);
            PlayerPrefs.DeleteKey(FullscreenKey);
            PlayerPrefs.DeleteKey(QualityKey);
            PlayerPrefs.DeleteKey(VSyncKey);
            PlayerPrefs.DeleteKey(FrameRateKey);
            PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.Save();
            ApplySaved();
        }

        public static void ApplySaved()
        {
            ApplyResolution();
            QualitySettings.SetQualityLevel(Quality, true);
            ApplyPerformance();
            AudioListener.volume = MasterVolume;
        }

        static void ApplyResolution()
        {
            ApplyResolution(Width, Height, Fullscreen);
        }

        public static void ApplyResolution(
            int width,
            int height,
            bool fullscreen)
        {
            FullScreenMode mode = fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.SetResolution(
                Mathf.Max(MinWidth, width),
                Mathf.Max(MinHeight, height),
                mode
            );
        }

        static void ApplyPerformance()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync
                ? -1
                : FrameRate <= 0 ? -1 : FrameRate;
        }
    }

    public class SettingsMenuUI : MonoBehaviour
    {
        [Header("Owner")]
        public PauseMenu pauseMenu;
        public bool showMainMenuButton;

        [Header("Resolution")]
        public Button previousResolutionButton;
        public Button nextResolutionButton;
        public TextMeshProUGUI resolutionValueText;
        public Toggle fullscreenToggle;

        [Header("Graphics")]
        public Button previousQualityButton;
        public Button nextQualityButton;
        public TextMeshProUGUI qualityValueText;

        [Header("Performance")]
        public Toggle vSyncToggle;
        public Button previousFrameRateButton;
        public Button nextFrameRateButton;
        public TextMeshProUGUI frameRateValueText;

        [Header("Audio")]
        public Slider masterVolumeSlider;
        public TextMeshProUGUI masterVolumeValueText;

        [Header("Commands")]
        public Button applyDisplayButton;
        public Button revertDisplayButton;
        public TextMeshProUGUI displayStatusText;
        public Button resetButton;
        public Button closeButton;
        public Button mainMenuButton;
        public Button quitButton;

        [Header("Frame Rate Options")]
        public List<int> frameRateOptions = new List<int>
        {
            30,
            60,
            120,
            144,
            0
        };
        public string unlimitedFrameRateLabel = "Sin limite";
        public string savedDisplayStatus = "Pantalla guardada";
        public string pendingDisplayStatus = "Cambios de pantalla sin aplicar";

        readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        int resolutionIndex;
        int qualityIndex;
        int frameRateIndex;
        bool pendingFullscreen;
        bool displayDirty;
        bool listenersBound;

        public bool HasPendingDisplayChanges => displayDirty;
        bool UsesDeferredDisplayApply =>
            applyDisplayButton != null ||
            revertDisplayButton != null ||
            displayStatusText != null;

        void Awake()
        {
            BindListeners();
        }

        void OnEnable()
        {
            BindListeners();
            Refresh();
        }

        public void Bind(PauseMenu owner)
        {
            pauseMenu = owner;
            Refresh();
        }

        public void Refresh()
        {
            BuildResolutions();
            resolutionIndex = FindResolutionIndex(
                GameSettings.Width,
                GameSettings.Height
            );
            qualityIndex = GameSettings.Quality;
            frameRateIndex = FindFrameRateIndex(GameSettings.FrameRate);
            pendingFullscreen = GameSettings.Fullscreen;
            displayDirty = false;

            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(pendingFullscreen);
            if (vSyncToggle != null)
                vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(
                    GameSettings.MasterVolume
                );
            if (mainMenuButton != null)
                mainMenuButton.gameObject.SetActive(showMainMenuButton);

            RefreshLabels();
        }

        void BindListeners()
        {
            if (listenersBound)
                return;
            listenersBound = true;

            previousResolutionButton?.onClick.AddListener(
                () => ChangeResolution(-1)
            );
            nextResolutionButton?.onClick.AddListener(
                () => ChangeResolution(1)
            );
            fullscreenToggle?.onValueChanged.AddListener(ChangeFullscreen);
            previousQualityButton?.onClick.AddListener(
                () => ChangeQuality(-1)
            );
            nextQualityButton?.onClick.AddListener(
                () => ChangeQuality(1)
            );
            vSyncToggle?.onValueChanged.AddListener(ChangeVSync);
            previousFrameRateButton?.onClick.AddListener(
                () => ChangeFrameRate(-1)
            );
            nextFrameRateButton?.onClick.AddListener(
                () => ChangeFrameRate(1)
            );
            masterVolumeSlider?.onValueChanged.AddListener(ChangeVolume);
            applyDisplayButton?.onClick.AddListener(ApplyDisplayChanges);
            revertDisplayButton?.onClick.AddListener(RevertDisplayChanges);
            resetButton?.onClick.AddListener(ResetDefaults);
            closeButton?.onClick.AddListener(Close);
            mainMenuButton?.onClick.AddListener(ReturnToMainMenu);
            quitButton?.onClick.AddListener(Quit);
        }

        void BuildResolutions()
        {
            resolutions.Clear();
            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
            {
                Vector2Int resolution = new Vector2Int(
                    available[i].width,
                    available[i].height
                );
                if (!resolutions.Contains(resolution))
                    resolutions.Add(resolution);
            }

            Vector2Int current = new Vector2Int(
                Mathf.Max(GameSettings.MinWidth, GameSettings.Width),
                Mathf.Max(GameSettings.MinHeight, GameSettings.Height)
            );
            if (!resolutions.Contains(current))
                resolutions.Add(current);
            AddFallbackResolution(1280, 720);
            AddFallbackResolution(1600, 900);
            AddFallbackResolution(1920, 1080);
            AddFallbackResolution(2560, 1440);
            resolutions.Sort((left, right) =>
            {
                int width = left.x.CompareTo(right.x);
                return width != 0 ? width : left.y.CompareTo(right.y);
            });
        }

        void AddFallbackResolution(int width, int height)
        {
            Vector2Int resolution = new Vector2Int(width, height);
            if (!resolutions.Contains(resolution))
                resolutions.Add(resolution);
        }

        int FindResolutionIndex(int width, int height)
        {
            int index = resolutions.FindIndex(
                value => value.x == width && value.y == height
            );
            return index >= 0 ? index : Mathf.Max(0, resolutions.Count - 1);
        }

        int FindFrameRateIndex(int value)
        {
            int index = frameRateOptions.IndexOf(value);
            return index >= 0 ? index : 1;
        }

        void ChangeResolution(int direction)
        {
            if (resolutions.Count == 0)
                return;

            resolutionIndex = Wrap(
                resolutionIndex + direction,
                resolutions.Count
            );
            if (UsesDeferredDisplayApply)
                MarkDisplayDirty();
            else
                ApplyDisplayChanges();
            RefreshLabels();
        }

        public void SelectPreviousResolution()
        {
            ChangeResolution(-1);
        }

        public void SelectNextResolution()
        {
            ChangeResolution(1);
        }

        void ChangeFullscreen(bool enabled)
        {
            pendingFullscreen = enabled;
            if (UsesDeferredDisplayApply)
                MarkDisplayDirty();
            else
                GameSettings.SaveDisplay(
                    GameSettings.Width,
                    GameSettings.Height,
                    pendingFullscreen
                );
            RefreshLabels();
        }

        public void SetPendingFullscreen(bool enabled)
        {
            ChangeFullscreen(enabled);
            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(enabled);
        }

        public void ApplyDisplayChanges()
        {
            if (resolutions.Count == 0)
                BuildResolutions();
            if (resolutions.Count == 0)
                return;

            Vector2Int resolution = resolutions[
                Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)
            ];
            GameSettings.SaveDisplay(
                resolution.x,
                resolution.y,
                pendingFullscreen
            );
            displayDirty = false;
            RefreshLabels();
        }

        public void RevertDisplayChanges()
        {
            BuildResolutions();
            resolutionIndex = FindResolutionIndex(
                GameSettings.Width,
                GameSettings.Height
            );
            pendingFullscreen = GameSettings.Fullscreen;
            displayDirty = false;
            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(pendingFullscreen);
            RefreshLabels();
        }

        void MarkDisplayDirty()
        {
            if (resolutions.Count == 0)
            {
                displayDirty = false;
                return;
            }

            Vector2Int resolution = resolutions[
                Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)
            ];
            displayDirty =
                resolution.x != GameSettings.Width ||
                resolution.y != GameSettings.Height ||
                pendingFullscreen != GameSettings.Fullscreen;
        }

        void ChangeQuality(int direction)
        {
            if (QualitySettings.names.Length == 0)
                return;

            qualityIndex = Wrap(
                qualityIndex + direction,
                QualitySettings.names.Length
            );
            GameSettings.SaveQuality(qualityIndex);
            RefreshLabels();
        }

        void ChangeVSync(bool enabled)
        {
            GameSettings.SaveVSync(enabled);
            RefreshLabels();
        }

        void ChangeFrameRate(int direction)
        {
            if (frameRateOptions.Count == 0)
                return;

            frameRateIndex = Wrap(
                frameRateIndex + direction,
                frameRateOptions.Count
            );
            GameSettings.SaveFrameRate(frameRateOptions[frameRateIndex]);
            RefreshLabels();
        }

        void ChangeVolume(float value)
        {
            GameSettings.SaveMasterVolume(value);
            RefreshLabels();
        }

        void ResetDefaults()
        {
            GameSettings.ResetDefaults();
            Refresh();
        }

        void Close()
        {
            if (displayDirty)
                RevertDisplayChanges();
            if (pauseMenu != null)
                pauseMenu.ResumeGame();
            else
                gameObject.SetActive(false);
        }

        void ReturnToMainMenu()
        {
            pauseMenu?.BackToMenu();
        }

        void Quit()
        {
            pauseMenu?.QuitGame();
        }

        void RefreshLabels()
        {
            if (resolutionValueText != null && resolutions.Count > 0)
            {
                Vector2Int resolution = resolutions[
                    Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)
                ];
                resolutionValueText.text =
                    resolution.x + " x " + resolution.y;
            }

            if (qualityValueText != null && QualitySettings.names.Length > 0)
            {
                qualityValueText.text = QualitySettings.names[
                    Mathf.Clamp(
                        qualityIndex,
                        0,
                        QualitySettings.names.Length - 1
                    )
                ];
            }

            if (frameRateValueText != null && frameRateOptions.Count > 0)
            {
                int frameRate = frameRateOptions[
                    Mathf.Clamp(frameRateIndex, 0, frameRateOptions.Count - 1)
                ];
                frameRateValueText.text = frameRate <= 0
                    ? unlimitedFrameRateLabel
                    : frameRate + " FPS";
            }

            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text =
                    Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
            }

            bool frameRateEnabled = !GameSettings.VSync;
            if (previousFrameRateButton != null)
                previousFrameRateButton.interactable = frameRateEnabled;
            if (nextFrameRateButton != null)
                nextFrameRateButton.interactable = frameRateEnabled;
            if (frameRateValueText != null)
                frameRateValueText.alpha = frameRateEnabled ? 1f : 0.45f;

            if (applyDisplayButton != null)
                applyDisplayButton.interactable = displayDirty;
            if (revertDisplayButton != null)
                revertDisplayButton.interactable = displayDirty;
            if (displayStatusText != null)
            {
                displayStatusText.text = displayDirty
                    ? pendingDisplayStatus
                    : savedDisplayStatus;
            }
        }

        static int Wrap(int value, int count)
        {
            if (count <= 0)
                return 0;
            return (value % count + count) % count;
        }
    }
}
