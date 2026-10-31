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

            // 3. Build Prefabs (v2.1 Balanced Stats)
            GameObject magmaBroadsword = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_MagmaBroadsword",
                "weapon_magma_broadsword",
                "Magma Broadsword",
                broadswordIcon,
                "Slashes in a 160° fiery arc (20 dmg, CD 1.1s, Burn 4 dmg/s).",
                "Primary Melee: Cleave & Burn DoT.",
                20f, 1.10f, 2.6f, 1.3f, 4.0f, 1.4f, 1, slashSwordFrames
            );

            GameObject battleAxe = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_BattleAxe",
                "weapon_battle_axe",
                "Battle Axe",
                battleAxeIcon,
                "Crushing shockwave cleave (34 dmg, CD 1.7s, Stagger 0.4s, KB 5.5).",
                "Heavy Melee: High impact crowd-control.",
                34f, 1.70f, 2.8f, 1.7f, 5.5f, 1.6f, 1, slashAxeFrames
            );

            GameObject twinDaggers = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_TwinDaggers",
                "weapon_twin_daggers",
                "Twin Daggers",
                twinDaggersIcon,
                "Rapid double strikes (12x2 dmg, CD 0.7s, Crit +8%). Targets Boss > Elite > Closest.",
                "Agile Melee: Anti-Boss & Elite execution.",
                12f, 0.70f, 1.8f, 1.1f, 2.0f, 1.25f, 2, slashDaggersFrames,
                30f, 0.12f
            );

            GameObject darkScythe = CreateMeleeWeaponPrefab(
                prefabsDir,
                "Weapon_DarkScythe",
                "weapon_dark_scythe",
                "Dark Scythe",
                darkScytheIcon,
                "Sweeps in a 270° reaping circle (24 dmg, CD 1.35s, Heals 1 HP/kill, max 5 HP/s).",
                "Wide Cleave: Decimates surrounding hordes & sustains HP.",
                24f, 1.35f, 3.0f, 2.2f, 4.5f, 1.8f, 1, slashScytheFrames
            );

            GameObject crystalLaser = CreateLaserWeaponPrefab(
                prefabsDir,
                "Weapon_CrystalLaser",
                "weapon_crystal_laser",
                "Crystal Laser",
                crystalLaserIcon,
                "Piercing laser beam melting straight lines of enemies (37 dmg, CD 1.75s, 7.0m).",
                "Piercing Beam: Infinite line pierce.",
                37f, 1.75f, 7.0f, 7.0f, 1.0f, 3.5f, crystalLaserFrames
            );

            GameObject crossbow = CreateRangedWeaponPrefab(
                prefabsDir,
                "Weapon_Crossbow",
                "weapon_crossbow",
                "Crossbow",
                crossbowIcon,
                "Fires piercing bolts (21 dmg, CD 0.95s, Pierce 2 with -25%/pierce). Targets Boss > Elite.",
                "Ranged Hunter: Auto-targets distant high-priority threats.",
                21f, 0.95f, 8.0f, 10.5f, 2, 1, 3.5f, energyOrbFrames
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
            float dmg, float cd, float range, float radius, float kb, float scale, int strikes, Sprite[] frames,
            float fps = 20f, float delay = 0.12f)
        {
            string path = $"{dir}/{prefabName}.prefab";
            GameObject go = new GameObject(prefabName);

            MeleeSlashWeapon w = go.AddComponent<MeleeSlashWeapon>();
            w.ConfigureMeleeWeapon(id, name, icon, desc, syn, dmg, cd, range, radius, kb, scale, strikes, frames, fps, delay);

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

            // Wire LevelUpManager (All 6 weapons in level-up pool & 9 Passives & 4 Backup Cards)
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

                SetPropSprite(soLum, "iconBookDamage", "Assets/PixelSurvivor/Items/Passives/item_book_damage.png");
                SetPropSprite(soLum, "iconFeatherSpeed", "Assets/PixelSurvivor/Items/Passives/item_feather_speed.png");
                SetPropSprite(soLum, "iconHeartLife", "Assets/PixelSurvivor/Items/Passives/item_heart_max_hp.png");
                SetPropSprite(soLum, "iconShieldArmor", "Assets/PixelSurvivor/Items/Passives/item_shield_armor.png");
                SetPropSprite(soLum, "iconClockCooldown", "Assets/PixelSurvivor/Items/Passives/item_clock_cooldown.png");
                SetPropSprite(soLum, "iconEmeraldWisdom", "Assets/PixelSurvivor/Items/Passives/item_emerald_exp.png");
                SetPropSprite(soLum, "iconEnderEye", "Assets/PixelSurvivor/Items/Passives/item_ender_eye_pickup.png");
                SetPropSprite(soLum, "iconPotionRegen", "Assets/PixelSurvivor/Items/Passives/item_potion_regen.png");
                SetPropSprite(soLum, "iconTotemRevive", "Assets/PixelSurvivor/Items/Passives/item_totem_revive.png");

                SetPropSprite(soLum, "iconHeartyMeal", "Assets/PixelSurvivor/Items/Drops/drop_cooked_steak.png");
                SetPropSprite(soLum, "iconEmeraldPouch", "Assets/PixelSurvivor/UI/Icons/icon_emerald.png");
                SetPropSprite(soLum, "iconScholarsTome", "Assets/PixelSurvivor/Items/Passives/item_book_damage.png");
                SetPropSprite(soLum, "iconBountyMark", "Assets/PixelSurvivor/Items/Drops/drop_lucky_block.png");

                soLum.ApplyModifiedProperties();
                EditorUtility.SetDirty(lum);
            }

            // Ensure Player has PassiveController
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null && pc.GetComponent<PassiveController>() == null)
            {
                pc.gameObject.AddComponent<PassiveController>();
                EditorUtility.SetDirty(pc.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=#10B981>[Weapon Setup]</color> Successfully wired weapons into {scenePath}");
        }

        private static void SetPropSprite(SerializedObject so, string propName, string assetPath)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = LoadSingleSprite(assetPath);
            }
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
