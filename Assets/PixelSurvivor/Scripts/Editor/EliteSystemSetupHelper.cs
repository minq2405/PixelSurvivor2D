using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelSurvivor.Combat;

namespace PixelSurvivor.Editor
{
    public static class EliteSystemSetupHelper
    {
        [MenuItem("PixelSurvivor/Setup Elite Enemy System (GDD Spec)", false, 117)]
        public static void SetupEliteEnemySystem()
        {
            Debug.Log("<color=#38BDF8>[Elite Setup]</color> Setting up Elite Enemy System per GDD Spec...");

            // 1. Ensure Config Directory
            string configDir = "Assets/PixelSurvivor/Configs";
            if (!AssetDatabase.IsValidFolder(configDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor", "Configs");
            }

            // 2. Create/Update Config_Normal.asset (v2.1 GDD)
            DifficultyEliteConfigSO normalConfig = CreateOrUpdateConfig(
                $"{configDir}/Config_Normal.asset",
                GameDifficulty.Normal,
                120f,   // 02:00
                0.03f,  // 3.0%
                2,      // Cap 2
                18f,    // 18s cd
                2.5f,   // HP x2.5
                1.30f,  // Dmg +30%
                1.00f,  // Spd 1.0x
                1.20f,  // Scale 1.20x
                0.20f,  // Chest 20%
                10      // Chest Cap 10
            );

            // 3. Create/Update Config_Hard.asset (v2.1 GDD)
            DifficultyEliteConfigSO hardConfig = CreateOrUpdateConfig(
                $"{configDir}/Config_Hard.asset",
                GameDifficulty.Hard,
                90f,    // 01:30
                0.05f,  // 5.0%
                4,      // Cap 4
                12f,    // 12s cd
                2.5f,   // HP x2.5
                1.30f,  // Dmg +30%
                1.00f,  // Spd 1.0x
                1.20f,  // Scale 1.20x
                0.17f,  // Chest 17%
                9       // Chest Cap 9
            );

            // 4. Create/Update Config_Hardcore.asset (v2.1 GDD)
            DifficultyEliteConfigSO hardcoreConfig = CreateOrUpdateConfig(
                $"{configDir}/Config_Hardcore.asset",
                GameDifficulty.Hardcore,
                60f,    // 01:00
                0.08f,  // 8.0%
                6,      // Cap 6
                8f,     // 8s cd
                2.5f,   // HP x2.5
                1.30f,  // Dmg +30%
                1.00f,  // Spd 1.0x
                1.20f,  // Scale 1.20x
                0.15f,  // Chest 15%
                8       // Chest Cap 8
            );

            AssetDatabase.SaveAssets();

            // 5. Build Item Drops: Steak, Magnet, TNT
            string prefabsDir = "Assets/PixelSurvivor/Items/Prefabs";
            Sprite spSteak = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_cooked_steak.png");
            Sprite spMagnet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_magnet.png");
            Sprite spTnt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_tnt.png");

            GameObject steakPrefab = CreateDropPrefab($"{prefabsDir}/Drop_Steak.prefab", spSteak, PickupType.Steak, 50, 1.35f, 0.5f);
            GameObject magnetPrefab = CreateDropPrefab($"{prefabsDir}/Drop_Magnet.prefab", spMagnet, PickupType.Magnet, 1, 1.35f, 0.5f);
            GameObject tntPrefab = CreateDropPrefab($"{prefabsDir}/Drop_Tnt.prefab", spTnt, PickupType.Tnt, 1, 1.35f, 0.5f);

            AssetDatabase.SaveAssets();

            // 6. Wire into Scenes
            WireScene("Assets/Scenes/MainGame.unity", normalConfig, hardConfig, hardcoreConfig, steakPrefab, magnetPrefab, tntPrefab);
            WireScene("Assets/Scenes/CombatSandbox.unity", normalConfig, hardConfig, hardcoreConfig, steakPrefab, magnetPrefab, tntPrefab);

            Debug.Log("<color=#10B981>[Elite Setup]</color> Elite Enemy System configured & wired successfully!");
        }

        private static DifficultyEliteConfigSO CreateOrUpdateConfig(
            string path, GameDifficulty diff, float unlockTime, float chance, int maxCap, float cd,
            float hpMul, float dmgMul, float spdMul, float scaleMul, float chestChance, int chestCap)
        {
            DifficultyEliteConfigSO config = AssetDatabase.LoadAssetAtPath<DifficultyEliteConfigSO>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DifficultyEliteConfigSO>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.difficulty = diff;
            config.unlockTimeInSeconds = unlockTime;
            config.eliteChance = chance;
            config.maxElitesOnScreen = maxCap;
            config.eliteCooldown = cd;
            config.hpMultiplier = hpMul;
            config.damageMultiplier = dmgMul;
            config.speedMultiplier = spdMul;
            config.scaleMultiplier = scaleMul;
            config.chestBaseChance = chestChance;
            config.maxChestsPerRun = chestCap;
            config.chestPityStep = 0.10f;
            config.chestMinGap = 20f;
            config.knockbackResistance = 0.25f;

            EditorUtility.SetDirty(config);
            return config;
        }

        private static GameObject CreateDropPrefab(string path, Sprite sprite, PickupType type, int value, float scale, float radius)
        {
            GameObject go = new GameObject(Path.GetFileNameWithoutExtension(path));
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            PickupItem pickup = go.AddComponent<PickupItem>();
            pickup.Initialize(type, value);

            SerializedObject soPickup = new SerializedObject(pickup);
            soPickup.FindProperty("itemType").enumValueIndex = (int)type;
            soPickup.FindProperty("value").intValue = value;
            soPickup.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void WireScene(string scenePath, DifficultyEliteConfigSO norm, DifficultyEliteConfigSO hard, DifficultyEliteConfigSO hcore, GameObject steak, GameObject magnet, GameObject tnt)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Wire WaveSpawner
            WaveSpawner ws = Object.FindAnyObjectByType<WaveSpawner>();
            if (ws != null)
            {
                SerializedObject soWs = new SerializedObject(ws);
                var propNorm = soWs.FindProperty("normalEliteConfig");
                var propHard = soWs.FindProperty("hardEliteConfig");
                var propHcore = soWs.FindProperty("hardcoreEliteConfig");

                if (propNorm != null) propNorm.objectReferenceValue = norm;
                if (propHard != null) propHard.objectReferenceValue = hard;
                if (propHcore != null) propHcore.objectReferenceValue = hcore;

                soWs.ApplyModifiedProperties();
                EditorUtility.SetDirty(ws);
            }

            // Wire ExperienceManager
            ExperienceManager em = Object.FindAnyObjectByType<ExperienceManager>();
            if (em != null)
            {
                SerializedObject soEm = new SerializedObject(em);
                var propSteak = soEm.FindProperty("steakPrefab");
                var propMag = soEm.FindProperty("magnetPrefab");
                var propTnt = soEm.FindProperty("tntPrefab");

                if (propSteak != null) propSteak.objectReferenceValue = steak;
                if (propMag != null) propMag.objectReferenceValue = magnet;
                if (propTnt != null) propTnt.objectReferenceValue = tnt;

                soEm.ApplyModifiedProperties();
                EditorUtility.SetDirty(em);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=#10B981>[Elite Setup]</color> Successfully wired Elite configs into {scenePath}");
        }
    }
}
