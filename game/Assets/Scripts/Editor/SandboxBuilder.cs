using System;
using System.IO;
using System.Linq;
using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Controls;
using GMR.Core.Feedback;
using GMR.Core.Heroes;
using GMR.Core.Rage;
using GMR.Core.Status;
using GMR.DevTools;
using GMR.Runtime.Abilities;
using GMR.Runtime.Controls;
using GMR.Runtime.Encounter;
using GMR.Runtime.Feedback;
using GMR.Runtime.Heroes;
using GMR.Runtime.Players;
using GMR.Runtime.UI;
using TMPro;
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
    /// Generates the Sandbox content (Epics 2-4): layers and the Physics2D layer matrix, placeholder sprites,
    /// Config/Definition assets (Heracles' kit incl. Wrath of Zeus, health, projectile, Shock stub, Combo and RAGE
    /// configs), the PlayerRoot, Hero and Projectile prefabs and the Sandbox scene (walls, two dummy attackers, three
    /// training dummies, feedback incl. Ultimate visuals, per-Player combat HUD, controls hint, pause/debug menu). Idempotent: existing Config/Definition assets are kept (tuning and slot edits
    /// survive; empty Hero fields are filled), prefabs and the scene contents are rebuilt.
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
        const string AbilitiesDir = "Assets/Data/Abilities";
        const string StatusesDir = "Assets/Data/Statuses";
        const string ConfigDir = "Assets/Data/Config";
        const string PrefabDir = "Assets/Prefabs";

        // Layer indices (Consistency Conventions: Player, Enemy, PlayerHitbox, EnemyHitbox, Projectile, Wall).
        const int PlayerLayer = 6;
        const int EnemyLayer = 7;
        const int PlayerHitboxLayer = 8;
        const int EnemyHitboxLayer = 9;
        const int ProjectileLayer = 10;
        const int WallLayer = 11;
        static readonly string[] LayerNames = { "Player", "Enemy", "PlayerHitbox", "EnemyHitbox", "Projectile", "Wall" };

        static readonly Color WallColor = new Color(0.33f, 0.36f, 0.44f, 1f);

        [MenuItem("GMR/Rebuild Sandbox (scene, prefabs, configs)")]
        public static void BuildAll()
        {
            // Open the scene FIRST: opening a scene unloads unused assets, which would turn asset references
            // loaded earlier in this method into destroyed objects (scene fields would serialize as null).
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TmpResources.EnsureAll(); // TMP Essential Resources + UI font, before the scene opens (see above)
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureFolder(HeroesDir);
            EnsureFolder(ConfigDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(AbilitiesDir);
            EnsureFolder(StatusesDir);
            EnsureLayers();
            EnsureLayerMatrix();

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
            var trainingConfig = EnsureAsset<TrainingDummyConfig>(ConfigDir + "/Config_TrainingDummy.asset");
            var heroHealth = EnsureAsset<HealthConfig>(ConfigDir + "/Config_Health_Heracles.asset", out bool heroHealthCreated);
            if (heroHealthCreated) SetAll(heroHealth, ("_maxHp", 100f), ("_knockbackResistance", 0f), ("_postHitInvulnerability", 0.5f));
            var dummyHealth = EnsureAsset<HealthConfig>(ConfigDir + "/Config_Health_TrainingDummy.asset", out bool dummyHealthCreated);
            if (dummyHealthCreated) SetAll(dummyHealth, ("_maxHp", 100f), ("_knockbackResistance", 0.5f), ("_postHitInvulnerability", 0f));

            var shock = EnsureAsset<StatusDefinition>(StatusesDir + "/Status_Shock.asset", out bool shockCreated);
            if (shockCreated) SetAll(shock, ("_id", "shock"), ("_displayName", "Shock"), ("_tint", new Color(0.6f, 0.85f, 1f, 1f)));
            var bolt = EnsureAsset<ProjectileConfig>(ConfigDir + "/Config_Projectile_Thunderbolt.asset", out bool boltCreated);
            if (boltCreated)
                SetAll(bolt, ("_speed", 14f), ("_lifetime", 0.8f), ("_radius", 0.2f), ("_pierce", 0),
                    ("_color", new Color(0.55f, 0.85f, 1f, 1f)));
            var (clubStrike, thunderbolt, stormWhirl) = EnsureHeraclesKit(shock, bolt);
            var wrathOfZeus = EnsureWrathOfZeus(shock);
            var combo = EnsureAsset<ComboConfig>(ConfigDir + "/Config_Combo.asset");
            var rage = EnsureAsset<RageConfig>(ConfigDir + "/Config_Rage_Heracles.asset");
            // Re-serialize existing tuning assets so fields added by later stories appear in the files (defaults kept).
            foreach (var asset in new Object[] { move, dash, aim, input, feedback, dummyConfig, trainingConfig, combo, rage })
                EditorUtility.SetDirty(asset);

            var hero = EnsureAsset<HeroDefinition>(HeroesDir + "/Hero_Heracles.asset", out bool heroCreated);
            if (heroCreated)
            {
                Set(hero, "_id", "heracles");
                Set(hero, "_displayName", "Heracles");
                Set(hero, "_placeholderColor", new Color(1f, 0.62f, 0.2f, 1f));
            }
            Set(hero, "_move", move);
            Set(hero, "_dash", dash);
            // Slot and health fields are filled only when empty, so slot swaps made by the team survive a rebuild.
            SetIfEmpty(hero, "_health", heroHealth);
            SetIfEmpty(hero, "_basic", clubStrike);
            SetIfEmpty(hero, "_special", thunderbolt);
            SetIfEmpty(hero, "_signature", stormWhirl);
            SetIfEmpty(hero, "_ultimate", wrathOfZeus);
            SetIfEmpty(hero, "_rage", rage);
            SetIfEmpty(hero, "_primaryStatus", shock);

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath)
                           ?? throw new InvalidOperationException("GameControls.inputactions not found at " + ControlsPath);

            var projectilePrefab = BuildProjectilePrefab(circle);
            var heroPrefab = BuildHeroPrefab(square, circle, ring, arrow, projectilePrefab);
            var playerPrefab = BuildPlayerRootPrefab(controls, hero, heroPrefab, input, aim, combo);
            BuildScene(scene, controls, playerPrefab, square, circle, ring, aim, input, feedback, dummyConfig,
                trainingConfig, dummyHealth, combo, rage);
            EnsureSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[GMR/Editor] Sandbox rebuilt (M5a).");
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

        /// <summary>Projectiles only meet enemies, walls and Default (Epic 3 deferred item: layer matrix).</summary>
        static void EnsureLayerMatrix()
        {
            Physics2D.IgnoreLayerCollision(ProjectileLayer, PlayerLayer, true);
            Physics2D.IgnoreLayerCollision(ProjectileLayer, PlayerHitboxLayer, true);
            Physics2D.IgnoreLayerCollision(ProjectileLayer, EnemyHitboxLayer, true);
            Physics2D.IgnoreLayerCollision(ProjectileLayer, ProjectileLayer, true);
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (settings.Length > 0) EditorUtility.SetDirty(settings[0]);
        }

        static (AbilityDefinition club, AbilityDefinition bolt, AbilityDefinition whirl) EnsureHeraclesKit(
            StatusDefinition shock, ProjectileConfig boltProjectile)
        {
            var club = EnsureAsset<AbilityDefinition>(AbilitiesDir + "/Ability_Heracles_ClubStrike.asset", out bool clubCreated);
            if (clubCreated)
                SetAll(club, ("_id", "heracles_club_strike"), ("_displayName", "Club Strike"),
                    ("_attackType", (int)AttackType.Thrust), ("_hitShape", (int)HitShape.Box),
                    ("_range", 1.3f), ("_width", 0.9f), ("_forwardOffset", 0.2f),
                    ("_startup", 0.08f), ("_active", 0.08f), ("_recovery", 0.22f), ("_cooldown", 0f),
                    ("_damage", 10f), ("_damageTags", (int)DamageTags.Physical), ("_hitWeight", (int)HitWeight.Heavy),
                    ("_knockback", 6f), ("_rootsCaster", false), ("_color", new Color(1f, 0.85f, 0.4f, 0.55f)));

            var bolt = EnsureAsset<AbilityDefinition>(AbilitiesDir + "/Ability_Heracles_Thunderbolt.asset", out bool boltCreated);
            if (boltCreated)
                SetAll(bolt, ("_id", "heracles_thunderbolt"), ("_displayName", "Thunderbolt"),
                    ("_attackType", (int)AttackType.Shot), ("_hitShape", (int)HitShape.Projectile),
                    ("_projectile", boltProjectile), ("_range", 0f), ("_width", 0f), ("_forwardOffset", 0f),
                    ("_startup", 0.1f), ("_active", 0.05f), ("_recovery", 0.2f), ("_cooldown", 1.5f),
                    ("_damage", 8f), ("_damageTags", (int)DamageTags.Shock), ("_hitWeight", (int)HitWeight.Light),
                    ("_knockback", 3f), ("_rootsCaster", false), ("_status", shock), ("_statusStacks", 1),
                    ("_color", new Color(0.55f, 0.85f, 1f, 0.6f)));

            var whirl = EnsureAsset<AbilityDefinition>(AbilitiesDir + "/Ability_Heracles_StormWhirl.asset", out bool whirlCreated);
            if (whirlCreated)
                SetAll(whirl, ("_id", "heracles_storm_whirl"), ("_displayName", "Storm Whirl"),
                    ("_attackType", (int)AttackType.Spin), ("_hitShape", (int)HitShape.Circle),
                    ("_range", 2.2f), ("_width", 0f), ("_forwardOffset", 0f),
                    ("_startup", 0.12f), ("_active", 0.3f), ("_recovery", 0.3f), ("_cooldown", 8f),
                    ("_damage", 20f), ("_damageTags", (int)DamageTags.Physical), ("_hitWeight", (int)HitWeight.Heavy),
                    ("_knockback", 7f), ("_rootsCaster", false), ("_status", shock), ("_statusStacks", 1),
                    ("_color", new Color(0.6f, 0.85f, 1f, 0.35f)));
            return (club, bolt, whirl);
        }

        /// <summary>Heracles' Ultimate (FR-17): lightning on every enemy; the Shock payload goes to the Status hook (Epic 5).</summary>
        static AbilityDefinition EnsureWrathOfZeus(StatusDefinition shock)
        {
            var wrath = EnsureAsset<AbilityDefinition>(AbilitiesDir + "/Ability_Heracles_WrathOfZeus.asset", out bool created);
            if (created)
                SetAll(wrath, ("_id", "heracles_wrath_of_zeus"), ("_displayName", "Wrath of Zeus"),
                    ("_attackType", (int)AttackType.Shot), ("_hitShape", (int)HitShape.AllEnemies),
                    ("_range", 0f), ("_width", 0f), ("_forwardOffset", 0f),
                    ("_startup", 0.35f), ("_active", 0.1f), ("_recovery", 0.4f), ("_cooldown", 0f),
                    ("_damage", 100f), ("_damageTags", (int)(DamageTags.Shock | DamageTags.Ultimate)),
                    ("_hitWeight", (int)HitWeight.Heavy), ("_knockback", 8f), ("_rootsCaster", true),
                    ("_status", shock), ("_statusStacks", 1), ("_color", new Color(0.7f, 0.9f, 1f, 0.6f)));
            return wrath;
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

        static Projectile BuildProjectilePrefab(Sprite circle)
        {
            var root = new GameObject("Projectile_Placeholder") { layer = ProjectileLayer };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic; // dynamic trigger: gets trigger events with static walls too
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f; // root is scaled to the configured diameter at launch
            collider.isTrigger = true;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.sortingOrder = 15;
            var projectile = root.AddComponent<Projectile>();
            Set(projectile, "_renderer", sr);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Projectile_Placeholder.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Projectile>();
        }

        static HeroRuntime BuildHeroPrefab(Sprite square, Sprite circle, Sprite ring, Sprite arrow, Projectile projectilePrefab)
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
            var boxShape = Child(root, "AbilityBoxShape", square, new Color(1f, 0.85f, 0.4f, 0.55f), 9, Vector2.zero, Vector2.one);
            boxShape.enabled = false;
            var circleShape = Child(root, "AbilityCircleShape", circle, new Color(0.6f, 0.85f, 1f, 0.35f), 2, Vector2.zero, Vector2.one);
            circleShape.enabled = false;

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
            var abilities = root.AddComponent<AbilityController>();
            Set(abilities, "_targetLayers", 1 << EnemyLayer);
            Set(abilities, "_wallLayers", 1 << WallLayer);
            Set(abilities, "_projectilePrefab", projectilePrefab);
            Set(abilities, "_boxShape", boxShape);
            Set(abilities, "_circleShape", circleShape);
            var flash = root.AddComponent<HitFlash>();
            Set(flash, "_renderer", bodyRenderer);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Hero_Placeholder.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<HeroRuntime>();
        }

        static GameObject BuildPlayerRootPrefab(InputActionAsset controls, HeroDefinition hero, HeroRuntime heroPrefab,
            InputConfig input, AimConfig aim, ComboConfig combo)
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
            Set(playerRoot, "_comboConfig", combo);

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
            AimConfig aim, InputConfig input, FeedbackConfig feedback, DummyAttackerConfig dummyConfig,
            TrainingDummyConfig trainingConfig, HealthConfig dummyHealth, ComboConfig combo, RageConfig rage)
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
            if (!camera.TryGetComponent(out CameraShake cameraShake)) cameraShake = camera.gameObject.AddComponent<CameraShake>();

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

            // UI: canvas, event system, flash, popups, help, pause menu. All text is TextMeshPro SDF on a Screen Space -
            // Overlay canvas, drawn at native resolution after the pixel-perfect world (only the world is pixel art).
            var font = new UiFont(TmpResources.Font, TmpResources.OutlineMaterial);
            var canvas = BuildCanvas();
            var eventSystem = BuildEventSystem(controls);

            var flash = UiImage(canvas.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform);
            var ultimateFlash = UiImage(canvas.transform, "UltimateFlash", new Color(0.75f, 0.9f, 1f, 0f));
            Stretch(ultimateFlash.rectTransform);

            // Popups and damage numbers: PopupSpawner projects world -> screen into this overlay container.
            var popupsRoot = new GameObject("Popups", typeof(RectTransform));
            popupsRoot.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)popupsRoot.transform);
            var template = UiText(popupsRoot.transform, "PopupTemplate", font, "PERFECT!", 34, TextAlignmentOptions.Center, Color.white, true);
            template.rectTransform.sizeDelta = new Vector2(400f, 56f);
            template.gameObject.SetActive(false);

            var help = UiText(canvas.transform, "ControlsHint", font,
                "Move: L-stick / WASD    DASH: A / Space    Direction Lock (hold): LT / Left Shift\n" +
                "Club Strike: X / J / LMB    Thunderbolt: Y / K / RMB    Storm Whirl: B / E    Wrath of Zeus (RAGE full): RT / R\n" +
                "Pause + debug menu: Start / Esc  (or Select / F1)    Menu: D-pad / stick / arrows, A / Enter, back: B / Esc",
                20, TextAlignmentOptions.BottomLeft, new Color(1f, 1f, 1f, 0.85f));
            help.textWrappingMode = TextWrappingModes.Normal;
            var helpRect = help.rectTransform;
            // Stretched along the bottom edge: wraps instead of running off narrower screens.
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0f, 0f);
            helpRect.offsetMin = new Vector2(12f, 8f);
            helpRect.offsetMax = new Vector2(-12f, 84f);

            BuildCombatHud(canvas.transform, font, bootstrap);

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
            var sfxSource = feedbackGo.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            var sfx = feedbackGo.AddComponent<SfxHookPlayer>();
            Set(sfx, "_config", feedback);
            Set(sfx, "_source", sfxSource);
            var router = feedbackGo.AddComponent<FeedbackRouter>();
            Set(router, "_bootstrap", bootstrap);
            Set(router, "_config", feedback);
            Set(router, "_popups", popups);
            Set(router, "_cameraShake", cameraShake);
            Set(router, "_sfx", sfx);
            var ultimateFx = feedbackGo.AddComponent<UltimateFeedback>();
            Set(ultimateFx, "_bootstrap", bootstrap);
            Set(ultimateFx, "_config", feedback);
            Set(ultimateFx, "_flash", ultimateFlash);
            Set(ultimateFx, "_square", square);
            Set(ultimateFx, "_circle", circle);
            Set(ultimateFx, "_ring", ring);

            var pauseGo = new GameObject("PauseController");
            var pause = pauseGo.AddComponent<PauseController>();
            BuildPauseMenu(canvas.transform, font, pause, bootstrap, driver, eventSystem, aim, input, feedback, combo, rage);

            BuildDummy("DummyAttacker_A", new Vector2(5f, 2.6f), 0f, bootstrap, dummyConfig, square, circle, ring);
            BuildDummy("DummyAttacker_B", new Vector2(-1.5f, -3.4f), 0.9f, bootstrap, dummyConfig, square, circle, ring);

            // Three training dummies in a triangle around (0.8, 0): Storm Whirl from the center reaches all three.
            BuildTrainingDummy("TrainingDummy_1", new Vector2(-0.3f, 0f), bootstrap, dummyHealth, trainingConfig, square);
            BuildTrainingDummy("TrainingDummy_2", new Vector2(1.3f, 1.0f), bootstrap, dummyHealth, trainingConfig, square);
            BuildTrainingDummy("TrainingDummy_3", new Vector2(1.3f, -1.0f), bootstrap, dummyHealth, trainingConfig, square);

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
            DummyAttackerConfig config, Sprite square, Sprite circle, Sprite ring)
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
            Set(dummy, "_areaRing", areaRing);
            Set(dummy, "_areaFill", areaFill);
            Set(dummy, "_startDelay", startDelay);
        }

        static void BuildTrainingDummy(string name, Vector2 position, EncounterBootstrap bootstrap, HealthConfig health,
            TrainingDummyConfig config, Sprite square)
        {
            var go = new GameObject(name) { layer = EnemyLayer };
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic; // knockback is an impulse (AD-16 ApplyResolved)
            body.gravityScale = 0f;
            body.mass = 3f;
            body.linearDamping = 6f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<CircleCollider2D>().radius = 0.45f;

            var visual = new GameObject("Visual") { layer = EnemyLayer };
            visual.transform.SetParent(go.transform, false);
            var bodyRenderer = Child(visual, "Body", square, new Color(0.86f, 0.74f, 0.5f, 1f), 3, Vector2.zero, new Vector2(0.75f, 0.9f));
            Child(visual, "Post", square, new Color(0.55f, 0.4f, 0.25f, 1f), 2, new Vector2(0f, -0.1f), new Vector2(0.15f, 1.1f));
            var barBack = Child(go, "HpBarBack", square, new Color(0.1f, 0.1f, 0.1f, 0.85f), 20, new Vector2(0f, 0.68f), new Vector2(1f, 0.12f));
            var barFill = Child(go, "HpBarFill", square, new Color(0.35f, 0.9f, 0.35f, 1f), 21, new Vector2(0f, 0.68f), new Vector2(1f, 0.12f));

            var dummy = go.AddComponent<TrainingDummy>();
            Set(dummy, "_bootstrap", bootstrap);
            Set(dummy, "_health", health);
            Set(dummy, "_config", config);
            Set(dummy, "_visualRoot", visual);
            var flash = go.AddComponent<HitFlash>();
            Set(flash, "_renderer", bodyRenderer);
            var bar = go.AddComponent<HealthBarView>();
            Set(bar, "_target", dummy);
            Set(bar, "_background", barBack);
            Set(bar, "_fill", barFill);
            Set(bar, "_width", 1f);
        }

        // ---------------------------------------------------------------- UI

        /// <summary>UI font asset + shared outline/shadow material preset.</summary>
        readonly struct UiFont
        {
            public readonly TMP_FontAsset Asset;
            public readonly Material Material;
            public UiFont(TMP_FontAsset asset, Material material) { Asset = asset; Material = material; }
        }

        /// <summary>
        /// Screen Space - Overlay: URP draws it on the back buffer after the pixel-perfect world, at native resolution.
        /// Authored for 1280x800 (Steam Deck 1:1; 1920x1080 scales ~1.42).
        /// </summary>
        static Canvas BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.layer = 5; // UI
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
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

        static void BuildPauseMenu(Transform canvas, UiFont font, PauseController pause, EncounterBootstrap bootstrap,
            TimeScaleServiceDriver driver, EventSystem eventSystem, AimConfig aim, InputConfig input, FeedbackConfig feedback,
            ComboConfig combo, RageConfig rage)
        {
            var dim = UiImage(canvas, "PauseMenu", new Color(0f, 0f, 0f, 0.6f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = UiImage(dim.transform, "Panel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            // 740 tall: fits the canvas height at 16:10 (800) and 16:9 (~759 reference px) with Resume, 10 toggles and the Fill RAGE cheat.
            panel.rectTransform.sizeDelta = new Vector2(580f, 740f);

            var title = UiText(panel.transform, "Title", font, "PAUSED", 34, TextAlignmentOptions.Center, Color.white, true);
            Place(title.rectTransform, 0f, 334f, 540f, 44f);

            var resume = BuildButton(panel.transform, font, "Resume", 284f);
            UnityEventTools.AddPersistentListener(resume.onClick, pause.Close);

            var header = UiText(panel.transform, "DebugHeader", font, "Debug toggles (session only)", 20, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.7f));
            Place(header.rectTransform, 0f, 240f, 540f, 28f);

            const float step = 40f;
            float y = 202f;
            var aimAssist = BuildToggle(panel.transform, font, "Aim Assist", y);
            var dirLock = BuildToggle(panel.transform, font, "Direction Lock", y -= step);
            var buffer = BuildToggle(panel.transform, font, "Input buffer", y -= step);
            var pdFlash = BuildToggle(panel.transform, font, "Perfect Dodge flash", y -= step);
            var pdSlow = BuildToggle(panel.transform, font, "Perfect Dodge time-slow", y -= step);
            var damageNumbers = BuildToggle(panel.transform, font, "Damage numbers", y -= step);
            var hitstop = BuildToggle(panel.transform, font, "Hitstop", y -= step);
            var shake = BuildToggle(panel.transform, font, "Screen shake", y -= step);
            var shrinking = BuildToggle(panel.transform, font, "Shrinking Combo window", y -= step);
            var rageDecay = BuildToggle(panel.transform, font, "RAGE decay", y -= step);
            var fillRage = BuildButton(panel.transform, font, "Fill RAGE (cheat)", y -= step + 12f);

            var footer = UiText(panel.transform, "Footer", font,
                "Navigate: D-pad / stick / arrows    Toggle: A / Enter\nClose: Start / Esc / B (back)",
                20, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
            Place(footer.rectTransform, 0f, -318f, 560f, 56f);

            var items = new UnityEngine.UI.Selectable[]
                { resume, aimAssist, dirLock, buffer, pdFlash, pdSlow, damageNumbers, hitstop, shake, shrinking, rageDecay, fillRage };
            LinkVertical(items);
            foreach (var item in items)
            {
                var cancel = item.gameObject.AddComponent<MenuCancelHandler>();
                Set(cancel, "_pause", pause);
            }

            var panelTools = dim.gameObject.AddComponent<DebugTogglePanel>();
            Set(panelTools, "_bootstrap", bootstrap);
            Set(panelTools, "_aimConfig", aim);
            Set(panelTools, "_inputConfig", input);
            Set(panelTools, "_feedbackConfig", feedback);
            Set(panelTools, "_comboConfig", combo);
            Set(panelTools, "_rageConfig", rage);
            Set(panelTools, "_shrinkingCombo", shrinking);
            Set(panelTools, "_rageDecay", rageDecay);
            Set(panelTools, "_fillRage", fillRage);
            Set(panelTools, "_aimAssist", aimAssist);
            Set(panelTools, "_directionLock", dirLock);
            Set(panelTools, "_inputBuffer", buffer);
            Set(panelTools, "_perfectDodgeFlash", pdFlash);
            Set(panelTools, "_perfectDodgeTimeSlow", pdSlow);
            Set(panelTools, "_damageNumbers", damageNumbers);
            Set(panelTools, "_hitstop", hitstop);
            Set(panelTools, "_screenShake", shake);

            Set(pause, "_bootstrap", bootstrap);
            Set(pause, "_timeScaleDriver", driver);
            Set(pause, "_eventSystem", eventSystem);
            Set(pause, "_menuRoot", dim.gameObject);
            Set(pause, "_firstSelected", resume);

            dim.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- combat HUD (Story 4.3)

        static readonly Color HudDark = new Color(0f, 0f, 0f, 0.55f);

        /// <summary>
        /// CombatHud with an inactive 600x172 panel template (reference pixels on the 1280x800 canvas; two panels side by
        /// side fit the width). Layout from the panel's top-left: title + seed, HP bar, RAGE meter, three slot boxes on
        /// the left; Combo on the right. No HUD text is smaller than 20 reference px.
        /// </summary>
        static void BuildCombatHud(Transform canvas, UiFont font, EncounterBootstrap bootstrap)
        {
            const float panelW = 600f, panelH = 172f;
            var hudRoot = new GameObject("CombatHud", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas, false);
            Stretch((RectTransform)hudRoot.transform);
            hudRoot.transform.SetSiblingIndex(0); // under popups, flashes and the pause menu

            var panelGo = new GameObject("HudPanelTemplate", typeof(RectTransform));
            panelGo.transform.SetParent(hudRoot.transform, false);
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(panelW, panelH);
            var background = panelGo.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.05f, 0.05f, 0.08f, 0.6f);
            background.raycastTarget = false;
            var panel = panelGo.AddComponent<PlayerHudPanel>();

            var title = HudText(panelGo.transform, "Title", font, "P1  HERACLES", 26, TextAlignmentOptions.Left, Color.white, 10f, -4f, 300f, 32f, true);
            var seed = HudText(panelGo.transform, "Seed", font, "SEED -", 20, TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.6f), 320f, -6f, 270f, 28f);
            seed.overflowMode = TextOverflowModes.Ellipsis;

            HudText(panelGo.transform, "HpLabel", font, "HP", 22, TextAlignmentOptions.Left, Color.white, 10f, -38f, 40f, 28f, true);
            var hpBack = HudImage(panelGo.transform, "HpBar", HudDark, 52f, -44f, 230f, 16f);
            var hpFill = FillChild(hpBack.transform, "Fill", new Color(0.35f, 0.9f, 0.35f, 1f));
            var hpText = HudText(panelGo.transform, "HpText", font, "100/100", 22, TextAlignmentOptions.Left, Color.white, 292f, -38f, 140f, 28f);

            var rageFrame = HudImage(panelGo.transform, "RageFrame", new Color(0f, 0f, 0f, 0.6f), 10f, -72f, 422f, 34f);
            var rageBack = HudImage(rageFrame.transform, "RageBar", new Color(0.12f, 0.05f, 0.05f, 0.9f), 2f, -2f, 418f, 30f);
            var rageFill = FillChild(rageBack.transform, "Fill", new Color(0.85f, 0.25f, 0.15f, 1f));
            rageFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            var rageLabel = HudText(rageBack.transform, "Label", font, "RAGE 0%", 22, TextAlignmentOptions.Center, Color.white, 0f, 0f, 418f, 30f, true);

            var slots = new HudSlotView[3];
            string[] glyphs = { "X", "Y", "B" };
            for (int i = 0; i < 3; i++)
            {
                var box = HudImage(panelGo.transform, "Slot" + glyphs[i], new Color(0.2f, 0.22f, 0.3f, 0.85f), 10f + i * 142f, -112f, 138f, 52f);
                var overlay = FillChild(box.transform, "Cooldown", new Color(0f, 0f, 0f, 0.6f));
                overlay.rectTransform.anchorMax = new Vector2(1f, 0f); // anchorMax.y = cooldown remaining fraction
                var name = HudText(box.transform, "Name", font, glyphs[i], 20, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f), 0f, -1f, 138f, 24f);
                name.overflowMode = TextOverflowModes.Ellipsis; // long ability names must not spill into the next box
                var value = HudText(box.transform, "Value", font, "READY", 22, TextAlignmentOptions.Center, Color.white, 0f, -24f, 138f, 28f, true);
                slots[i] = new HudSlotView { Glyph = glyphs[i], Name = name, Value = value, CooldownOverlay = overlay.rectTransform, Background = box };
            }

            HudText(panelGo.transform, "ComboLabel", font, "COMBO", 20, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.75f), 446f, -38f, 146f, 26f, true);
            var comboCount = HudText(panelGo.transform, "ComboCount", font, "x0", 44, TextAlignmentOptions.Center, Color.white, 446f, -62f, 146f, 50f, true);
            var comboBack = HudImage(panelGo.transform, "ComboWindow", HudDark, 456f, -116f, 126f, 10f);
            var comboFill = FillChild(comboBack.transform, "Fill", new Color(1f, 0.85f, 0.35f, 1f));
            comboFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            var resetCue = HudText(panelGo.transform, "ResetCue", font, "", 22, TextAlignmentOptions.Center, new Color(1f, 0.3f, 0.25f, 1f), 446f, -132f, 146f, 30f, true);

            Set(panel, "_title", title);
            Set(panel, "_seed", seed);
            Set(panel, "_hpFill", hpFill.rectTransform);
            Set(panel, "_hpFillImage", hpFill);
            Set(panel, "_hpText", hpText);
            Set(panel, "_comboCount", comboCount);
            Set(panel, "_comboWindowFill", comboFill.rectTransform);
            Set(panel, "_comboWindowImage", comboFill);
            Set(panel, "_comboResetCue", resetCue);
            Set(panel, "_rageFill", rageFill.rectTransform);
            Set(panel, "_rageFillImage", rageFill);
            Set(panel, "_rageFrame", rageFrame);
            Set(panel, "_rageLabel", rageLabel);
            var so = new SerializedObject(panel);
            var slotsProp = so.FindProperty("_slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                var e = slotsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Glyph").stringValue = slots[i].Glyph;
                e.FindPropertyRelative("Name").objectReferenceValue = slots[i].Name;
                e.FindPropertyRelative("Value").objectReferenceValue = slots[i].Value;
                e.FindPropertyRelative("CooldownOverlay").objectReferenceValue = slots[i].CooldownOverlay;
                e.FindPropertyRelative("Background").objectReferenceValue = slots[i].Background;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            panelGo.SetActive(false);

            var hud = hudRoot.AddComponent<CombatHud>();
            Set(hud, "_bootstrap", bootstrap);
            Set(hud, "_container", hudRoot.transform);
            Set(hud, "_template", panel);
            Set(hud, "_margin", new Vector2(8f, 8f));
            Set(hud, "_rowStep", panelH + 8f);
        }

        /// <summary>Top-left anchored rect at (x, y) from the parent's top-left, size (w, h), reference pixels.</summary>
        static void TopLeft(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static UnityEngine.UI.Image HudImage(Transform parent, string name, Color color, float x, float y, float w, float h)
        {
            var image = UiImage(parent, name, color);
            TopLeft(image.rectTransform, x, y, w, h);
            return image;
        }

        /// <summary>A fill stretched over its parent; PlayerHudPanel moves anchorMax to show a fraction.</summary>
        static UnityEngine.UI.Image FillChild(Transform parent, string name, Color color)
        {
            var image = UiImage(parent, name, color);
            Stretch(image.rectTransform);
            return image;
        }

        static TextMeshProUGUI HudText(Transform parent, string name, UiFont font, string content, float size,
            TextAlignmentOptions alignment, Color color, float x, float y, float w, float h, bool bold = false)
        {
            var text = UiText(parent, name, font, content, size, alignment, color, bold);
            TopLeft(text.rectTransform, x, y, w, h);
            return text;
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

        static UnityEngine.UI.Button BuildButton(Transform parent, UiFont font, string label, float y)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0f, y, 440f, 48f);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.colors = RowColors();
            var text = UiText(go.transform, "Label", font, label, 26, TextAlignmentOptions.Center, Color.white, true);
            Stretch(text.rectTransform);
            return button;
        }

        static UnityEngine.UI.Toggle BuildToggle(Transform parent, UiFont font, string label, float y)
        {
            var go = new GameObject(label.Replace(" ", "") + "Toggle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0f, y, 500f, 36f);
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
            boxRect.anchoredPosition = new Vector2(8f, 0f);
            boxRect.sizeDelta = new Vector2(28f, 28f);

            var check = UiImage(box.transform, "Checkmark", new Color(0.1f, 0.1f, 0.1f, 1f));
            check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            check.rectTransform.sizeDelta = new Vector2(28f, 28f);
            toggle.graphic = check;

            var text = UiText(go.transform, "Label", font, label, 24, TextAlignmentOptions.Left, Color.white);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(48f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
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

        /// <summary>
        /// TextMeshPro SDF text with the shared outline + shadow preset: stays sharp at any canvas scale (no legacy
        /// bitmap font, no Outline effect copies).
        /// </summary>
        static TextMeshProUGUI UiText(Transform parent, string name, UiFont font, string content, float size,
            TextAlignmentOptions alignment, Color color, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font.Asset;
            text.fontSharedMaterial = font.Material;
            text.text = content;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
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
                case Vector2 v: p.vector2Value = v; break;
                case int i when p.propertyType == SerializedPropertyType.LayerMask: p.intValue = i; break;
                case int i: p.intValue = i; break;
                case null: p.objectReferenceValue = null; break;
                default: throw new NotSupportedException(value.GetType().Name);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetAll(Object target, params (string property, object value)[] values)
        {
            foreach (var (property, value) in values) Set(target, property, value);
        }

        /// <summary>Sets an object reference only if it is empty (keeps edits made in the Inspector).</summary>
        static void SetIfEmpty(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property) ?? throw new InvalidOperationException($"{target.GetType().Name}.{property} not found.");
            if (p.objectReferenceValue != null) return;
            p.objectReferenceValue = value;
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
