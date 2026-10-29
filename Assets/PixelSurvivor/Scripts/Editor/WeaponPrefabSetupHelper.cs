using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelSurvivor.Combat;
using PixelSurvivor.UI;

namespace PixelSurvivor.Editor
{
    public static class WeaponPrefabSetupHelper
    {
        [MenuItem("PixelSurvivor/Setup Weapon Prefabs (All 6 Weapons)", false, 116)]
        public static void SetupWeaponPrefabs()
        {
            Debug.Log("<color=#38BDF8>[Weapon Setup]</color> Building all 6 Weapon Prefabs and wiring Systems...");

            string prefabsDir = "Assets/PixelSurvivor/Weapons/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor/Weapons", "Prefabs");
            }

            // 1. Load Icons
            Sprite broadswordIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_magma_broadsword.png");
            Sprite battleAxeIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_battle_axe.png");
            Sprite twinDaggersIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_twin_daggers.png");
            Sprite darkScytheIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_dark_scythe.png");
            Sprite crystalLaserIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_crystal_laser.png");
            Sprite crossbowIcon = LoadSingleSprite("Assets/PixelSurvivor/Weapons/weapon_crossbow.png");

            // 2. Load VFX Sprites
            Sprite[] slashSwordFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/Slashes/vfx_slash_sword.png");
            Sprite[] slashAxeFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/Slashes/vfx_slash_axe.png");
            Sprite[] slashDaggersFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/Slashes/vfx_slash_daggers.png");
            Sprite[] slashScytheFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/Slashes/vfx_slash_scythe.png");
            Sprite[] crystalLaserFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_crystal_laser.png");
            Sprite[] energyOrbFrames = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_energy_orb.png");

            Debug.Log($"[Weapon Setup] Loaded VFX: Sword({slashSwordFrames.Length}), Axe({slashAxeFrames.Length}), Daggers({slashDaggersFrames.Length}), Scythe({slashScytheFrames.Length}), Laser({crystalLaserFrames.Length}), CrossbowOrb({energyOrbFrames.Length})");

            // 3. Build Prefabs
            GameObject magmaBroadsword = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_MagmaBroadsword",
                "weapon_magma_broadsword",
                "Magma Broadsword",
                broadswordIcon,
                "Slashes forward in a wide fiery arc, dealing solid contact damage.",
                "Primary Melee: Balanced frontline area cleave.",
                25f, 1.1f, 2.2f, 1.3f, 4.5f, 1.4f, 1, slashSwordFrames
            );

            GameObject battleAxe = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_BattleAxe",
                "weapon_battle_axe",
                "Battle Axe",
                battleAxeIcon,
                "Delivers a crushing cleave that blasts enemies backward with massive knockback.",
                "Heavy Melee: High damage & crowd-clearing knockback.",
                45f, 1.65f, 2.5f, 1.7f, 7.0f, 1.6f, 1, slashAxeFrames
            );

            GameObject twinDaggers = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_TwinDaggers",
                "weapon_twin_daggers",
                "Twin Daggers",
                twinDaggersIcon,
                "Rapidly slashes twice in quick succession with blinding agility.",
                "Agile Melee: High attack speed & combo strikes.",
                14f, 0.65f, 1.8f, 1.1f, 2.5f, 1.25f, 2, slashDaggersFrames
            );

            GameObject darkScythe = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_DarkScythe",
                "weapon_dark_scythe",
                "Dark Scythe",
                darkScytheIcon,
                "Sweeps in an immense 270-degree reaping arc around the player.",
                "Wide Cleave: Decimates surrounding hordes.",
                32f, 1.35f, 2.8f, 2.2f, 5.0f, 1.8f, 1, slashScytheFrames
            );

            GameObject crystalLaser = CreateLaserWeaponPrefab(
                prefabsDir,
                "Weapon_CrystalLaser",
                "weapon_crystal_laser",
                "Crystal Laser",
                crystalLaserIcon,
                "Emits an intense piercing laser beam that melts through entire lines of enemies.",
                "Piercing Beam: Pierces through unlimited enemies in a line.",
                22f, 1.75f, 8.5f, 8.5f, 1.0f, 3.5f, crystalLaserFrames
            );

            GameObject crossbow = CreateRangedWeaponPrefab(
                prefabsDir,
                "Weapon_Crossbow",
                "weapon_crossbow",
                "Crossbow",
                crossbowIcon,
                "Fires high-velocity piercing bolts that seek out and puncture nearby enemies.",
                "Ranged Hunter: Auto-targets distant threats with piercing bolts.",
                24f, 0.95f, 9.0f, 10.5f, 2, 1, 4.0f, energyOrbFrames
            );

            AssetDatabase.SaveAssets();

            GameObject[] allWeapons = new GameObject[] { magmaBroadsword, battleAxe, twinDaggers, darkScythe, crystalLaser, crossbow };

            // 4. Wire into Scenes
            WireSceneWeapons("Assets/Scenes/MainGame.unity", magmaBroadsword, twinDaggers, crossbow, allWeapons);
            WireSceneWeapons("Assets/Scenes/CombatSandbox.unity", magmaBroadsword, twinDaggers, crossbow, allWeapons);

            Debug.Log("<color=#10B981>[Weapon Setup]</color> All 6 Weapon Prefabs created, wired to Characters (Phương án A) and Level Up Pool!");
        }

        private static GameObject CreateMeleeWeaponPrefab(
            string dir, string prefabName, string id, string name, Sprite icon, string desc, string syn,
            float dmg, float cd, float range, float radius, float kb, float scale, int strikes, Sprite[] frames)
        {
            string path = $"{dir}/{prefabName}.prefab";
            GameObject go = new GameObject(prefabName);

            MeleeSlashWeapon w = go.AddComponent<MeleeSlashWeapon>();
            w.ConfigureMeleeWeapon(id, name, icon, desc, syn, dmg, cd, range, radius, kb, scale, strikes, frames);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Weapon Setup]</color> Created Melee Weapon Prefab: {name} at {path}");
            return prefab;
        }

        private static GameObject CreateLaserWeaponPrefab(
            string dir, string prefabName, string id, string name, Sprite icon, string desc, string syn,
            float dmg, float cd, float range, float length, float thickness, float kb, Sprite[] frames)
        {
            string path = $"{dir}/{prefabName}.prefab";
            GameObject go = new GameObject(prefabName);

            LaserBeamWeapon w = go.AddComponent<LaserBeamWeapon>();
            w.ConfigureLaserWeapon(id, name, icon, desc, syn, dmg, cd, range, length, thickness, frames);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Weapon Setup]</color> Created Laser Weapon Prefab: {name} at {path}");
            return prefab;
        }

        private static GameObject CreateRangedWeaponPrefab(
            string dir, string prefabName, string id, string name, Sprite icon, string desc, string syn,
            float dmg, float cd, float range, float speed, int pierce, int count, float kb, Sprite[] frames)
        {
            string path = $"{dir}/{prefabName}.prefab";
            GameObject go = new GameObject(prefabName);

            RangedProjectileWeapon w = go.AddComponent<RangedProjectileWeapon>();
            w.ConfigureRangedWeapon(id, name, icon, desc, syn, dmg, cd, range, speed, pierce, count, frames);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Weapon Setup]</color> Created Ranged Weapon Prefab: {name} at {path}");
            return prefab;
        }

        private static void WireSceneWeapons(string scenePath, GameObject steveW, GameObject alexW, GameObject villagerW, GameObject[] allWeapons)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Wire MainMenuManager (Starting weapons: Steve, Alex, Villager)
            MainMenuManager mmm = Object.FindAnyObjectByType<MainMenuManager>();
            if (mmm != null)
            {
                SerializedObject soMmm = new SerializedObject(mmm);
                var propSteve = soMmm.FindProperty("steveStartingWeaponPrefab");
                if (propSteve != null) propSteve.objectReferenceValue = steveW;

                var propAlex = soMmm.FindProperty("alexStartingWeaponPrefab");
                if (propAlex != null) propAlex.objectReferenceValue = alexW;

                var propVillager = soMmm.FindProperty("villagerStartingWeaponPrefab");
                if (propVillager != null) propVillager.objectReferenceValue = villagerW;

                soMmm.ApplyModifiedProperties();
                EditorUtility.SetDirty(mmm);
            }

            // Wire LevelUpManager (All 6 weapons in level-up pool)
            LevelUpManager lum = Object.FindAnyObjectByType<LevelUpManager>();
            if (lum != null)
            {
                SerializedObject soLum = new SerializedObject(lum);
                var propAll = soLum.FindProperty("allWeaponPrefabs");
                if (propAll != null)
                {
                    propAll.ClearArray();
                    propAll.arraySize = allWeapons.Length;
                    for (int i = 0; i < allWeapons.Length; i++)
                    {
                        propAll.GetArrayElementAtIndex(i).objectReferenceValue = allWeapons[i];
                    }
                }
                soLum.ApplyModifiedProperties();
                EditorUtility.SetDirty(lum);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=#10B981>[Weapon Setup]</color> Successfully wired weapons into {scenePath}");
        }

        private static Sprite LoadSingleSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return all.Length > 0 ? all[0] : null;
        }

        private static Sprite[] LoadSprites(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            return assets.OfType<Sprite>()
                .OrderBy(s => {
                    int idx = s.name.LastIndexOf('_');
                    int val = 0;
                    if (idx >= 0 && int.TryParse(s.name.Substring(idx + 1), out val)) return val;
                    return 0;
                })
                .ToArray();
        }
    }
}
