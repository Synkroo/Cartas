#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoDeCartas.Enemies;
using JuegoDeCartas.Missions;
using JuegoDeCartas.UI;

public static class MissionSystemSetup
{
    const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string MissionAssetFolder = "Assets/Scripts/Misiones/Misiones S.O";
    const string MissionPrefabFolder = "Assets/Scripts/Prefabs";
    const string MissionPrefabPath = MissionPrefabFolder + "/MisionPrefab.prefab";

    [MenuItem("Tools/Juego de Cartas/Rebuild Mission System")]
    public static void RebuildMissionSystem()
    {
        EnsureFolders();
        MissionEntryUI prefab = CreateMissionPrefab();
        List<MissionData> missions = CreateMissionAssets();
        SetupMainMenuScene(prefab, missions);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Scripts/Misiones");
        EnsureFolder(MissionAssetFolder);
        EnsureFolder(MissionPrefabFolder);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static MissionEntryUI CreateMissionPrefab()
    {
        GameObject root = CreateUiObject("MisionPrefab", null, typeof(Image), typeof(Button), typeof(MissionEntryUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300f, 460f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.16f, 0.16f, 0.16f, 1f);

        GameObject glow = CreateUiObject("Glow", rootRect, typeof(Image));
        Stretch(glow.GetComponent<RectTransform>());
        Image glowImage = glow.GetComponent<Image>();
        glowImage.color = new Color(1f, 1f, 1f, 0f);
        glowImage.raycastTarget = false;

        GameObject selected = CreateUiObject("Selected", rootRect, typeof(Image));
        Stretch(selected.GetComponent<RectTransform>(), -6f);
        Image selectedImage = selected.GetComponent<Image>();
        selectedImage.color = new Color(1f, 1f, 1f, 0.16f);
        selectedImage.raycastTarget = false;
        selected.SetActive(false);

        GameObject imageFrame = CreateUiObject("ImageFrame", rootRect, typeof(Image));
        RectTransform imageFrameRect = imageFrame.GetComponent<RectTransform>();
        imageFrameRect.anchorMin = new Vector2(0.5f, 1f);
        imageFrameRect.anchorMax = new Vector2(0.5f, 1f);
        imageFrameRect.pivot = new Vector2(0.5f, 1f);
        imageFrameRect.anchoredPosition = new Vector2(0f, -24f);
        imageFrameRect.sizeDelta = new Vector2(252f, 150f);
        imageFrame.GetComponent<Image>().color = new Color(0.28f, 0.28f, 0.28f, 1f);

        GameObject missionImage = CreateUiObject("MissionImage", imageFrameRect, typeof(Image));
        Stretch(missionImage.GetComponent<RectTransform>(), -8f);
        Image missionImageComponent = missionImage.GetComponent<Image>();
        missionImageComponent.color = Color.white;
        missionImageComponent.preserveAspect = true;

        TextMeshProUGUI title = CreateText("Title", rootRect, "Mision", 30f, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -188f);
        titleRect.sizeDelta = new Vector2(-36f, 52f);

        TextMeshProUGUI description = CreateText("Description", rootRect, "Descripcion", 18f, TextAlignmentOptions.TopLeft);
        RectTransform descriptionRect = description.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -244f);
        descriptionRect.sizeDelta = new Vector2(-42f, 96f);

        TextMeshProUGUI difficulty = CreateText("Difficulty", rootRect, "Dificultad", 18f, TextAlignmentOptions.Center);
        RectTransform difficultyRect = difficulty.GetComponent<RectTransform>();
        difficultyRect.anchorMin = new Vector2(0.5f, 0f);
        difficultyRect.anchorMax = new Vector2(0.5f, 0f);
        difficultyRect.pivot = new Vector2(0.5f, 0f);
        difficultyRect.anchoredPosition = new Vector2(0f, 72f);
        difficultyRect.sizeDelta = new Vector2(180f, 28f);

        Image[] squares = new Image[3];
        Button[] difficultyButtons = new Button[3];
        GameObject[] medals = new GameObject[3];
        for (int i = 0; i < squares.Length; i++)
        {
            GameObject square = CreateUiObject("Difficulty" + (i + 1), rootRect, typeof(Image), typeof(Button));
            RectTransform squareRect = square.GetComponent<RectTransform>();
            squareRect.anchorMin = new Vector2(0.5f, 0f);
            squareRect.anchorMax = new Vector2(0.5f, 0f);
            squareRect.pivot = new Vector2(0.5f, 0.5f);
            squareRect.anchoredPosition = new Vector2(-34f + (i * 34f), 54f);
            squareRect.sizeDelta = new Vector2(20f, 20f);
            squares[i] = square.GetComponent<Image>();
            squares[i].color = new Color(1f, 1f, 1f, 0.25f);
            difficultyButtons[i] = square.GetComponent<Button>();

            GameObject medal = CreateUiObject("Medal" + (i + 1), rootRect, typeof(Image));
            RectTransform medalRect = medal.GetComponent<RectTransform>();
            medalRect.anchorMin = new Vector2(0.5f, 0f);
            medalRect.anchorMax = new Vector2(0.5f, 0f);
            medalRect.pivot = new Vector2(0.5f, 0.5f);
            medalRect.anchoredPosition = new Vector2(-18f + (i * 34f), 66f);
            medalRect.sizeDelta = new Vector2(12f, 12f);
            medal.GetComponent<Image>().color = new Color(1f, 0.82f, 0.24f, 1f);
            medal.SetActive(false);
            medals[i] = medal;
        }

        TextMeshProUGUI completed = CreateText("Completed", rootRect, "Completada", 18f, TextAlignmentOptions.Center);
        RectTransform completedRect = completed.GetComponent<RectTransform>();
        completedRect.anchorMin = new Vector2(0f, 0f);
        completedRect.anchorMax = new Vector2(1f, 0f);
        completedRect.pivot = new Vector2(0.5f, 0f);
        completedRect.anchoredPosition = new Vector2(0f, 18f);
        completedRect.sizeDelta = new Vector2(-40f, 26f);

        MissionEntryUI entry = root.GetComponent<MissionEntryUI>();
        entry.button = root.GetComponent<Button>();
        entry.titleText = title;
        entry.descriptionText = description;
        entry.difficultyText = difficulty;
        entry.completedText = completed;
        entry.missionImage = missionImageComponent;
        entry.glowImage = glowImage;
        entry.selectedState = selected;
        entry.difficultySquares = squares;
        entry.difficultyButtons = difficultyButtons;
        entry.difficultyMedals = medals;

        PrefabUtility.SaveAsPrefabAsset(root, MissionPrefabPath);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<MissionEntryUI>(MissionPrefabPath);
    }

    static List<MissionData> CreateMissionAssets()
    {
        EnemyData boss = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Scripts/Enemigos/Enemy S.O/Bosses/KingSlime.asset");
        AssetDatabase.DeleteAsset(MissionAssetFolder + "/Mision_01.asset");
        AssetDatabase.DeleteAsset(MissionAssetFolder + "/Mision_02.asset");
        AssetDatabase.DeleteAsset(MissionAssetFolder + "/Mision_03.asset");

        EnemyData rojo = LoadEnemy("Normales/SlimeRojo.asset");
        EnemyData naranja = LoadEnemy("Normales/SlimeNaranja.asset");
        EnemyData amarillo = LoadEnemy("Normales/SlimeAmarillo.asset");
        EnemyData verde = LoadEnemy("Normales/SlimeVerde.asset");
        EnemyData azul = LoadEnemy("Normales/SlimeAzul.asset");
        EnemyData morado = LoadEnemy("Normales/SlimeMorado.asset");

        EnemyData fuego = LoadEnemy("MiniBosses/SlimeDeFuego.asset");
        EnemyData hielo = LoadEnemy("MiniBosses/SlimeDeHielo.asset");
        EnemyData naturaleza = LoadEnemy("MiniBosses/SlimeDeNaturaleza.asset");
        EnemyData dorado = LoadEnemy("MiniBosses/SlimeDorado.asset");

        return new List<MissionData>
        {
            CreateMission("Mision_Slimes.asset", "Derrotar a los slimes",
                "Derrota a todos los slimes del tablon y acaba con King Slime al final de la run.",
                null, boss, 10,
                new List<EnemyData> { rojo, naranja, amarillo, verde, azul, morado },
                new List<EnemyData> { fuego, hielo, naturaleza, dorado })
        };
    }

    static EnemyData LoadEnemy(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Scripts/Enemigos/Enemy S.O/" + relativePath);
    }

    static MissionData CreateMission(
        string fileName,
        string missionName,
        string description,
        Sprite image,
        EnemyData boss,
        int combats,
        List<EnemyData> normalEnemies,
        List<EnemyData> miniBosses)
    {
        string path = MissionAssetFolder + "/" + fileName;
        MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(path);
        if (mission == null)
        {
            mission = ScriptableObject.CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(mission, path);
        }

        mission.missionName = missionName;
        mission.description = description;
        mission.image = image;
        mission.boss = boss;
        mission.combatCount = combats;
        mission.miniBossFrequency = 3;
        mission.possibleNormalEnemies = normalEnemies;
        mission.possibleMiniBosses = miniBosses;
        EditorUtility.SetDirty(mission);
        return mission;
    }

    static void SetupMainMenuScene(MissionEntryUI prefab, List<MissionData> missions)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        MainMenuManager menuManager = Object.FindFirstObjectByType<MainMenuManager>();
        if (canvas == null || menuManager == null)
            throw new System.InvalidOperationException("MainMenu necesita Canvas y MainMenuManager.");

        GameObject oldPanel = GameObject.Find("MisionesPanel");
        if (oldPanel != null)
            Object.DestroyImmediate(oldPanel);

        GameObject panel = CreateUiObject("MisionesPanel", canvas.transform, typeof(Image), typeof(CanvasGroup), typeof(MissionSelectionMenu));
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        TextMeshProUGUI title = CreateText("Titulo Misiones", panel.transform, "Misiones", 48f, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(-80f, 70f);

        GameObject scrollView = CreateUiObject("ScrollView", panel.transform, typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(70f, 120f);
        scrollRectTransform.offsetMax = new Vector2(-70f, -120f);
        scrollView.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

        GameObject content = CreateUiObject("Content", scrollRectTransform, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 470f);

        HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 36f;
        layout.padding = new RectOffset(40, 40, 10, 10);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;

        Button startButton = CreateButton("Boton Comenzar", panel.transform, "Selecciona una mision", new Vector2(0f, 48f), new Vector2(360f, 64f));
        Button backButton = CreateButton("Boton Volver", panel.transform, "Volver", new Vector2(-500f, 48f), new Vector2(220f, 58f));

        MissionSelectionMenu missionMenu = panel.GetComponent<MissionSelectionMenu>();
        missionMenu.panel = panel;
        missionMenu.canvasGroup = group;
        missionMenu.content = contentRect;
        missionMenu.missionPrefab = prefab;
        missionMenu.startButton = startButton;
        missionMenu.startButtonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
        missionMenu.missions = missions;

        RemovePersistentListeners(backButton.onClick);
        UnityEventTools.AddPersistentListener(backButton.onClick, missionMenu.Close);

        SerializedObject serializedMenuManager = new SerializedObject(menuManager);
        SerializedProperty missionMenuProperty = serializedMenuManager.FindProperty("missionMenu");
        missionMenuProperty.objectReferenceValue = missionMenu;
        serializedMenuManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menuManager);

        Button playButton = GameObject.Find("Button Jugar")?.GetComponent<Button>();
        if (playButton != null)
        {
            RemovePersistentListeners(playButton.onClick);
            UnityEventTools.AddPersistentListener(playButton.onClick, menuManager.PlayGame);
            EditorUtility.SetDirty(playButton);
        }

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        List<System.Type> allComponents = new List<System.Type> { typeof(RectTransform) };
        allComponents.AddRange(components);
        GameObject gameObject = new GameObject(name, allComponents.ToArray());
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        gameObject.layer = LayerMask.NameToLayer("UI");
        return gameObject;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        return textComponent;
    }

    static Button CreateButton(string name, Transform parent, string text, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject gameObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        gameObject.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        TextMeshProUGUI label = CreateText("Text", rectTransform, text, 24f, TextAlignmentOptions.Center);
        Stretch(label.GetComponent<RectTransform>());
        return gameObject.GetComponent<Button>();
    }

    static void RemovePersistentListeners(UnityEngine.Events.UnityEvent unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(unityEvent, i);
    }

    static void Stretch(RectTransform rectTransform, float inset = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(-inset, -inset);
        rectTransform.offsetMax = new Vector2(inset, inset);
    }

    static Sprite LoadFirstSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
#endif
