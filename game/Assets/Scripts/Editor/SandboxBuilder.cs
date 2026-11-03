using System;
using System.IO;
using System.Linq;
using GMR.Core.Controls;
using GMR.Core.Feedback;
using GMR.Core.Heroes;
using GMR.DevTools;
using GMR.Runtime.Controls;
using GMR.Runtime.Encounter;
using GMR.Runtime.Feedback;
using GMR.Runtime.Heroes;
using GMR.Runtime.Players;
using GMR.Runtime.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace GMR.Editor
{
    /// <summary>
    /// Generates the M4a content (Epic 2): layers, placeholder sprites, Config/Definition assets, the PlayerRoot and
    /// Hero prefabs and the Sandbox scene (walls, two dummy attackers, HUD hint, pause/debug menu). Idempotent:
    /// existing Config assets are kept (tuning edits survive), prefabs and the scene contents are rebuilt.
    /// Batchmode: unity run game -- -executeMethod GMR.Editor.SandboxBuilder.BuildAll
    /// </summary>
    public static class SandboxBuilder
    {
        const string ScenePath = "Assets/Scenes/Sandbox.unity";
        const string ControlsPath = "Assets/Input/GameControls.inputactions";
        const string ArtDir = "Assets/Art/Placeholder";
        const string SquarePath = ArtDir + "/Placeholder_Square.png";
        const string CirclePath = ArtDir + "/Placeholder_Circle.png";
        const string RingPath = ArtDir + "/Placeholder_Ring.png";
        const string ArrowPath = ArtDir + "/Placeholder_Arrow.png";
        const string HeroesDir = "Assets/Data/Heroes";
        const string ConfigDir = "Assets/Data/Config";
        const string PrefabDir = "Assets/Prefabs";

        // Layer indices (Consistency Conventions: Player, Enemy, PlayerHitbox, EnemyHitbox, Projectile, Wall).
        const int PlayerLayer = 6;
        const int EnemyLayer = 7;
        const int WallLayer = 11;
        static readonly string[] LayerNames = { "Player", "Enemy", "PlayerHitbox", "EnemyHitbox", "Projectile", "Wall" };

        static readonly Color WallColor = new Color(0.33f, 0.36f, 0.44f, 1f);

        [MenuItem("GMR/Rebuild M4a Sandbox (scene, prefabs, configs)")]
        public static void BuildAll()
        {
            // Open the scene FIRST: opening a scene unloads unused assets, which would turn asset references
            // loaded earlier in this method into destroyed objects (scene fields would serialize as null).
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureFolder(HeroesDir);
            EnsureFolder(ConfigDir);
            EnsureFolder(PrefabDir);
            EnsureLayers();

            var square = LoadSprite(SquarePath);
            var circle = EnsureShapeSprite(CirclePath, 32, (x, y, s) => Inside(x, y, s, s * 0.5f));
            var ring = EnsureShapeSprite(RingPath, 64, (x, y, s) => Inside(x, y, s, s * 0.5f) && !Inside(x, y, s, s * 0.5f - 3f));
            var arrow = EnsureShapeSprite(ArrowPath, 16, ArrowPixel);

            var move = EnsureAsset<MoveConfig>(ConfigDir + "/Config_Move_Heracles.asset");
            var dash = EnsureAsset<DashConfig>(ConfigDir + "/Config_Dash_Heracles.asset");
            var aim = EnsureAsset<AimConfig>(ConfigDir + "/Config_Aim.asset");
            var input = EnsureAsset<InputConfig>(ConfigDir + "/Config_Input.asset");
            var feedback = EnsureAsset<FeedbackConfig>(ConfigDir + "/Config_Feedback.asset");
            var dummyConfig = EnsureAsset<DummyAttackerConfig>(ConfigDir + "/Config_DummyAttacker.asset");
            var hero = EnsureAsset<HeroDefinition>(HeroesDir + "/Hero_Heracles.asset", out bool heroCreated);
            if (heroCreated)
            {
                Set(hero, "_id", "heracles");
                Set(hero, "_displayName", "Heracles");
                Set(hero, "_placeholderColor", new Color(1f, 0.62f, 0.2f, 1f));
            }
            Set(hero, "_move", move);
            Set(hero, "_dash", dash);

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath)
                           ?? throw new InvalidOperationException("GameControls.inputactions not found at " + ControlsPath);

            var heroPrefab = BuildHeroPrefab(square, ring, arrow);
            var playerPrefab = BuildPlayerRootPrefab(controls, hero, heroPrefab, input, aim);
            BuildScene(scene, controls, playerPrefab, square, circle, ring, aim, input, feedback, dummyConfig);
            EnsureSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[GMR/Editor] M4a Sandbox rebuilt.");
        }

        // ---------------------------------------------------------------- project setup

        static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < LayerNames.Length; i++)
            {
                var p = layers.GetArrayElementAtIndex(PlayerLayer + i);
                if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = LayerNames[i];
                else if (p.stringValue != LayerNames[i])
                    throw new InvalidOperationException($"Layer {PlayerLayer + i} is '{p.stringValue}', expected '{LayerNames[i]}'.");
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static T EnsureAsset<T>(string path) where T : ScriptableObject => EnsureAsset<T>(path, out _);

        static T EnsureAsset<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------------------------------------------------------------- sprites

        static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException("Sprite not found: " + path);

        static bool Inside(int x, int y, int size, float radius)
        {
            float c = (size - 1) * 0.5f;
            float dx = x - c, dy = y - c;
            return dx * dx + dy * dy <= radius * radius;
        }

        static bool ArrowPixel(int x, int y, int size)
        {
            // Right-pointing triangle: base on the left edge, tip on the right.
            float half = (size - 1) * 0.5f;
            float t = x / (float)(size - 1);
            return Mathf.Abs(y - half) <= half * (1f - t) + 0.5f;
        }

        static Sprite EnsureShapeSprite(string path, int size, Func<int, int, int, bool> filled)
        {
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = filled(x, y, size) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != size ||
                         importer.filterMode != FilterMode.Point;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size; // every generated shape is exactly 1 unit across
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return LoadSprite(path);
        }

        // ---------------------------------------------------------------- prefabs

        static HeroRuntime BuildHeroPrefab(Sprite square, Sprite ring, Sprite arrow)
        {
            var root = new GameObject("Hero_Placeholder") { layer = PlayerLayer };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearDamping = 0f;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            var bodyRenderer = Child(root, "Body", square, Color.white, 10, Vector2.zero, new Vector2(0.8f, 0.8f));
            var arrowRenderer = Child(root, "AimArrow", arrow, Color.white, 12, new Vector2(0.85f, 0f), new Vector2(0.5f, 0.5f));
            var assist = Child(root, "AssistMarker", ring, new Color(0.4f, 1f, 1f, 0.85f), 4, Vector2.zero, new Vector2(1.3f, 1.3f));
            assist.enabled = false;
            var bar = Child(root, "RecoveryBar", square, new Color(0.55f, 0.75f, 1f, 1f), 11, new Vector2(0f, -0.62f), new Vector2(1f, 0.12f));
            bar.enabled = false;

            var heroRuntime = root.AddComponent<HeroRuntime>();
            Set(heroRuntime, "_body", bodyRenderer);
            root.AddComponent<HeroMover>();
            var dash = root.AddComponent<DashController>();
            Set(dash, "_passThroughWhileDashing", 1 << EnemyLayer);
            var visuals = root.AddComponent<HeroVisuals>();
            Set(visuals, "_recoveryBar", bar);
            var indicator = root.AddComponent<AimIndicator>();
            Set(indicator, "_hero", heroRuntime);
            Set(indicator, "_arrow", arrowRenderer);
            Set(indicator, "_assistMarker", assist);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Hero_Placeholder.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<HeroRuntime>();
        }

        static GameObject BuildPlayerRootPrefab(InputActionAsset controls, HeroDefinition hero, HeroRuntime heroPrefab,
            InputConfig input, AimConfig aim)
        {
            var root = new GameObject("PlayerRoot");
            // Order matters: PlayerRoot and the reader must exist before PlayerInput announces the join.
            var playerRoot = root.AddComponent<PlayerRoot>();
            var reader = root.AddComponent<PlayerInputReader>();
            var playerInput = root.AddComponent<PlayerInput>();

            Set(reader, "_controls", controls);
            Set(playerRoot, "_reader", reader);
            Set(playerRoot, "_startingHero", hero);
            Set(playerRoot, "_heroPrefab", heroPrefab);
            Set(playerRoot, "_inputConfig", input);
            Set(playerRoot, "_aimConfig", aim);

            var so = new SerializedObject(playerInput);
            so.FindProperty("m_Actions").objectReferenceValue = controls;
            so.FindProperty("m_NotificationBehavior").intValue = (int)PlayerNotifications.InvokeCSharpEvents;
            so.FindProperty("m_DefaultActionMap").stringValue = "Gameplay";
            so.FindProperty("m_NeverAutoSwitchControlSchemes").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/PlayerRoot.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static SpriteRenderer Child(GameObject parent, string name, Sprite sprite, Color color, int order, Vector2 localPos, Vector2 scale)
        {
            var go = new GameObject(name) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        // ---------------------------------------------------------------- scene

        static void BuildScene(UnityEngine.SceneManagement.Scene scene, InputActionAsset controls, GameObject playerPrefab, Sprite square, Sprite circle, Sprite ring,
            AimConfig aim, InputConfig input, FeedbackConfig feedback, DummyAttackerConfig dummyConfig)
        {
            Camera camera = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Main Camera") camera = go.GetComponent<Camera>();
                else Object.DestroyImmediate(go);
            }
            if (camera == null) throw new InvalidOperationException("Sandbox has no 'Main Camera'.");
            camera.backgroundColor = new Color(0.09f, 0.09f, 0.12f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Bootstrap: context, player join, time scale.
            var bootGo = new GameObject("EncounterBootstrap");
            var spawn = new GameObject("SpawnPoint_P0");
            spawn.transform.position = new Vector3(-6f, 0f, 0f);
            spawn.transform.SetParent(bootGo.transform, true);
            var pim = bootGo.AddComponent<PlayerInputManager>();
            var pimSo = new SerializedObject(pim);
            pimSo.FindProperty("m_PlayerPrefab").objectReferenceValue = playerPrefab;
            pimSo.FindProperty("m_JoinBehavior").intValue = (int)PlayerJoinBehavior.JoinPlayersManually;
            pimSo.FindProperty("m_NotificationBehavior").intValue = (int)PlayerNotifications.InvokeCSharpEvents;
            pimSo.ApplyModifiedPropertiesWithoutUndo();
            var bootstrap = bootGo.AddComponent<EncounterBootstrap>();
            Set(bootstrap, "_playerInputManager", pim);
            SetArray(bootstrap, "_spawnPoints", new Object[] { spawn.transform });
            var driver = bootGo.AddComponent<TimeScaleServiceDriver>();
            Set(driver, "_bootstrap", bootstrap);

            BuildArena(square);

            // UI: canvas, event system, flash, popups, help, pause menu.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = BuildCanvas();
            var eventSystem = BuildEventSystem(controls);

            var flash = UiImage(canvas.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform);

            var popupsRoot = new GameObject("Popups", typeof(RectTransform));
            popupsRoot.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)popupsRoot.transform);
            var template = UiText(popupsRoot.transform, "PopupTemplate", font, "PERFECT!", 18, TextAnchor.MiddleCenter, Color.white);
            template.fontStyle = FontStyle.Bold;
            template.rectTransform.sizeDelta = new Vector2(200f, 30f);
            template.gameObject.SetActive(false);

            var help = UiText(canvas.transform, "ControlsHint", font,
                "Move: L-stick / WASD    DASH: A / Space    Direction Lock (hold): LT / Left Shift\n" +
                "Pause + debug menu: Start / Esc  (or Select / F1)    Menu: D-pad, stick or arrows + A / Enter",
                9, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.8f));
            var helpRect = help.rectTransform;
            helpRect.anchorMin = helpRect.anchorMax = helpRect.pivot = new Vector2(0f, 0f);
            helpRect.anchoredPosition = new Vector2(6f, 4f);
            helpRect.sizeDelta = new Vector2(600f, 28f);

            var feedbackGo = new GameObject("Feedback");
            var popups = feedbackGo.AddComponent<PopupSpawner>();
            Set(popups, "_camera", camera);
            Set(popups, "_container", popupsRoot.transform);
            Set(popups, "_template", template);
            Set(popups, "_config", feedback);
            var pdFeedback = feedbackGo.AddComponent<PerfectDodgeFeedback>();
            Set(pdFeedback, "_bootstrap", bootstrap);
            Set(pdFeedback, "_config", feedback);
            Set(pdFeedback, "_flash", flash);
            Set(pdFeedback, "_popups", popups);

            var pauseGo = new GameObject("PauseController");
            var pause = pauseGo.AddComponent<PauseController>();
            BuildPauseMenu(canvas.transform, font, pause, bootstrap, driver, eventSystem, aim, input, feedback);

            BuildDummy("DummyAttacker_A", new Vector2(3f, 1.5f), 0f, bootstrap, dummyConfig, popups, square, circle, ring);
            BuildDummy("DummyAttacker_B", new Vector2(-1.5f, -3.2f), 0.9f, bootstrap, dummyConfig, popups, square, circle, ring);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save " + ScenePath);
        }

        static void BuildArena(Sprite square)
        {
            var arena = new GameObject("Arena");
            // View is 20 x 12.5 units (640x400 @ 32 PPU); walls frame it, two pillars give cover.
            Wall(arena, "Wall_Top", new Vector2(0f, 6.0f), new Vector2(20f, 0.5f), square);
            Wall(arena, "Wall_Bottom", new Vector2(0f, -6.0f), new Vector2(20f, 0.5f), square);
            Wall(arena, "Wall_Left", new Vector2(-9.75f, 0f), new Vector2(0.5f, 12.5f), square);
            Wall(arena, "Wall_Right", new Vector2(9.75f, 0f), new Vector2(0.5f, 12.5f), square);
            Wall(arena, "Pillar_A", new Vector2(-4.5f, 2.8f), new Vector2(1.5f, 1.5f), square);
            Wall(arena, "Pillar_B", new Vector2(6f, -2.5f), new Vector2(1f, 2.5f), square);
        }

        static void Wall(GameObject parent, string name, Vector2 position, Vector2 size, Sprite square)
        {
            var go = new GameObject(name) { layer = WallLayer };
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = WallColor;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        static void BuildDummy(string name, Vector2 position, float startDelay, EncounterBootstrap bootstrap,
            DummyAttackerConfig config, PopupSpawner popups, Sprite square, Sprite circle, Sprite ring)
        {
            var go = new GameObject(name) { layer = EnemyLayer };
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.45f; // static body: blocks walking, DASH passes through
            Child(go, "Body", square, new Color(0.62f, 0.25f, 0.75f, 1f), 3, Vector2.zero, new Vector2(0.9f, 0.9f));
            var areaRing = Child(go, "AreaRing", ring, new Color(1f, 0.5f, 0.2f, 0.55f), 2, Vector2.zero, Vector2.one);
            var areaFill = Child(go, "AreaFill", circle, new Color(1f, 0.45f, 0.1f, 0.45f), 1, Vector2.zero, Vector2.zero);
            areaFill.enabled = false;

            var dummy = go.AddComponent<DummyAttacker>();
            Set(dummy, "_bootstrap", bootstrap);
            Set(dummy, "_config", config);
            Set(dummy, "_popups", popups);
            Set(dummy, "_areaRing", areaRing);
            Set(dummy, "_areaFill", areaFill);
            Set(dummy, "_startDelay", startDelay);
        }

        // ---------------------------------------------------------------- UI

        static Canvas BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.layer = 5; // UI
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 400f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvas;
        }

        static EventSystem BuildEventSystem(InputActionAsset controls)
        {
            var go = new GameObject("EventSystem");
            var eventSystem = go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            // Serialized fields directly: the actionsAsset setter re-resolves references in ways that fail in batchmode.
            var so = new SerializedObject(module);
            so.FindProperty("m_ActionsAsset").objectReferenceValue = controls;
            so.FindProperty("m_MoveAction").objectReferenceValue = ActionRef("UI", "Navigate");
            so.FindProperty("m_SubmitAction").objectReferenceValue = ActionRef("UI", "Submit");
            so.FindProperty("m_CancelAction").objectReferenceValue = ActionRef("UI", "Cancel");
            so.FindProperty("m_PointAction").objectReferenceValue = ActionRef("UI", "Point");
            so.FindProperty("m_LeftClickAction").objectReferenceValue = ActionRef("UI", "Click");
            foreach (var unused in new[] { "m_MiddleClickAction", "m_RightClickAction", "m_ScrollWheelAction",
                         "m_TrackedDevicePositionAction", "m_TrackedDeviceOrientationAction" })
                so.FindProperty(unused).objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            return eventSystem;
        }

        static InputActionReference ActionRef(string map, string action)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(ControlsPath).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.name == action && r.action.actionMap?.name == map);
            return reference != null ? reference : throw new InvalidOperationException($"No InputActionReference for {map}/{action}.");
        }

        static void BuildPauseMenu(Transform canvas, Font font, PauseController pause, EncounterBootstrap bootstrap,
            TimeScaleServiceDriver driver, EventSystem eventSystem, AimConfig aim, InputConfig input, FeedbackConfig feedback)
        {
            var dim = UiImage(canvas, "PauseMenu", new Color(0f, 0f, 0f, 0.6f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = UiImage(dim.transform, "Panel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            panel.rectTransform.sizeDelta = new Vector2(280f, 260f);

            var title = UiText(panel.transform, "Title", font, "PAUSED", 18, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, 0f, 105f, 260f, 26f);

            var resume = BuildButton(panel.transform, font, "Resume", 70f);
            UnityEventTools.AddPersistentListener(resume.onClick, pause.Close);

            var header = UiText(panel.transform, "DebugHeader", font, "Debug toggles (session only)", 10, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.6f));
            Place(header.rectTransform, 0f, 44f, 260f, 16f);

            var aimAssist = BuildToggle(panel.transform, font, "Aim Assist", 22f);
            var dirLock = BuildToggle(panel.transform, font, "Direction Lock", -2f);
            var buffer = BuildToggle(panel.transform, font, "Input buffer", -26f);
            var pdFlash = BuildToggle(panel.transform, font, "Perfect Dodge flash", -50f);
            var pdSlow = BuildToggle(panel.transform, font, "Perfect Dodge time-slow", -74f);

            var footer = UiText(panel.transform, "Footer", font, "Navigate: D-pad / stick / arrows    Toggle: A / Enter\nClose: Start / Esc",
                9, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            Place(footer.rectTransform, 0f, -108f, 260f, 26f);

            LinkVertical(resume, aimAssist, dirLock, buffer, pdFlash, pdSlow);

            var panelTools = dim.gameObject.AddComponent<DebugTogglePanel>();
            Set(panelTools, "_bootstrap", bootstrap);
            Set(panelTools, "_aimConfig", aim);
            Set(panelTools, "_inputConfig", input);
            Set(panelTools, "_feedbackConfig", feedback);
            Set(panelTools, "_aimAssist", aimAssist);
            Set(panelTools, "_directionLock", dirLock);
            Set(panelTools, "_inputBuffer", buffer);
            Set(panelTools, "_perfectDodgeFlash", pdFlash);
            Set(panelTools, "_perfectDodgeTimeSlow", pdSlow);

            Set(pause, "_bootstrap", bootstrap);
            Set(pause, "_timeScaleDriver", driver);
            Set(pause, "_eventSystem", eventSystem);
            Set(pause, "_menuRoot", dim.gameObject);
            Set(pause, "_firstSelected", resume);

            dim.gameObject.SetActive(false);
        }

        static UnityEngine.UI.ColorBlock RowColors() => new UnityEngine.UI.ColorBlock
        {
            normalColor = new Color(0.25f, 0.27f, 0.35f, 0.7f),
            highlightedColor = new Color(0.9f, 0.75f, 0.3f, 0.9f),
            pressedColor = new Color(1f, 0.6f, 0.1f, 1f),
            selectedColor = new Color(1f, 0.8f, 0.2f, 1f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.05f,
        };

        static UnityEngine.UI.Button BuildButton(Transform parent, Font font, string label, float y)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0f, y, 220f, 24f);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.colors = RowColors();
            var text = UiText(go.transform, "Label", font, label, 13, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        static UnityEngine.UI.Toggle BuildToggle(Transform parent, Font font, string label, float y)
        {
            var go = new GameObject(label.Replace(" ", "") + "Toggle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0f, y, 240f, 20f);
            var row = go.AddComponent<UnityEngine.UI.Image>();
            row.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            row.type = UnityEngine.UI.Image.Type.Sliced;
            var toggle = go.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = row; // whole row lights up when selected: easy to see with gamepad/keyboard
            toggle.colors = RowColors();

            var box = UiImage(go.transform, "Box", Color.white);
            box.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            box.type = UnityEngine.UI.Image.Type.Sliced;
            var boxRect = box.rectTransform;
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(4f, 0f);
            boxRect.sizeDelta = new Vector2(14f, 14f);

            var check = UiImage(box.transform, "Checkmark", new Color(0.1f, 0.1f, 0.1f, 1f));
            check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            check.rectTransform.sizeDelta = new Vector2(14f, 14f);
            toggle.graphic = check;

            var text = UiText(go.transform, "Label", font, label, 12, TextAnchor.MiddleLeft, Color.white);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 0f);
            textRect.offsetMax = new Vector2(-4f, 0f);
            toggle.isOn = true;
            return toggle;
        }

        static void LinkVertical(params UnityEngine.UI.Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i].navigation = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length],
                };
            }
        }

        static UnityEngine.UI.Image UiImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static UnityEngine.UI.Text UiText(Transform parent, string name, Font font, string content, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // ---------------------------------------------------------------- serialized-field helpers

        static void Set(Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property) ?? throw new InvalidOperationException($"{target.GetType().Name}.{property} not found.");
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case string s: p.stringValue = s; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case Color c: p.colorValue = c; break;
                case int i when p.propertyType == SerializedPropertyType.LayerMask: p.intValue = i; break;
                case int i: p.intValue = i; break;
                case null: p.objectReferenceValue = null; break;
                default: throw new NotSupportedException(value.GetType().Name);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property) ?? throw new InvalidOperationException($"{target.GetType().Name}.{property} not found.");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
