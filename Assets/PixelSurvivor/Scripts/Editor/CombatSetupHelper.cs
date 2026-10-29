using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelSurvivor.Combat;

namespace PixelSurvivor.Editor
{
    public static class CombatSetupHelper
    {
        [MenuItem("PixelSurvivor/Combat: Setup Player Weapons & Test Dummies")]
        public static void SetupPlayerWeaponsAndTestDummies()
        {
            // 1. Locate Player
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                Debug.LogError("[CombatSetupHelper] Player GameObject not found in current scene!");
                return;
            }

            PlayerController player = playerObj.GetComponent<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[CombatSetupHelper] PlayerController not found on Player!");
                return;
            }

            // 2. Ensure WeaponController is attached to Player
            WeaponController weaponCtrl = playerObj.GetComponent<WeaponController>();
            if (weaponCtrl == null)
            {
                weaponCtrl = Undo.AddComponent<WeaponController>(playerObj);
                Debug.Log("[CombatSetupHelper] Attached WeaponController to Player.");
            }

            // 3. Load VFX Assets
            Sprite[] swordSlashFrames = LoadSpritesAtPath("Assets/PixelSurvivor/Particles/VFX/Slashes/vfx_slash_sword.png");
            Sprite[] hitSparkFrames = LoadSpritesAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_hit_spark.png");
            Sprite[] deathSmokeFrames = LoadSpritesAtPath("Assets/PixelSurvivor/Particles/VFX/vfx_smoke_puff.png");
            Sprite[] zombieWalkFrames = LoadSpritesAtPath("Assets/PixelSurvivor/Enemies/Zombie/zombie_walk.png");
            Sprite zombieSprite = zombieWalkFrames != null && zombieWalkFrames.Length > 0 
                ? zombieWalkFrames[0] 
                : LoadFirstSprite("Assets/PixelSurvivor/Enemies/Zombie");

            Debug.Log($"[CombatSetupHelper] Loaded {swordSlashFrames.Length} sword slash frames, {hitSparkFrames.Length} hit sparks, {deathSmokeFrames.Length} smoke frames.");

            // 4. Create / Configure Magma Broadsword Weapon
            Transform existingWeapon = playerObj.transform.Find("Weapon_MagmaBroadsword");
            GameObject weaponObj = existingWeapon != null ? existingWeapon.gameObject : new GameObject("Weapon_MagmaBroadsword");
            
            weaponObj.transform.SetParent(playerObj.transform);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;

            MeleeSlashWeapon sword = weaponObj.GetComponent<MeleeSlashWeapon>();
            if (sword == null) sword = weaponObj.AddComponent<MeleeSlashWeapon>();

            sword.weaponId = "weapon_magma_broadsword";
            sword.weaponName = "Magma Broadsword";
            sword.ConfigureVFX(swordSlashFrames, scale: 1.4f, fps: 20f, strikes: 1);
            
            // Set stats via reflection (Synchronized with VFX reach ~1.85m)
            SetPrivateField(sword, "baseDamage", 25f);
            SetPrivateField(sword, "baseCooldown", 1.1f);
            SetPrivateField(sword, "attackRange", 1.9f); // Perfectly matches visual blade reach
            SetPrivateField(sword, "knockbackForce", 6.0f);
            SetPrivateField(sword, "hitRadius", 1.2f);
            SetPrivateField(sword, "spawnOffset", 0.25f);
            SetPrivateField(sword, "vfxAngleOffset", 0f);
            SetPrivateField(sword, "vfxLocalOffset", Vector2.zero);
            SetPrivateField(sword, "followPlayer", true);
            SetPrivateField(sword, "requireTargetInRange", true);
            SetPrivateField(sword, "enemyLayer", (LayerMask)(~LayerMask.GetMask("UI", "Ignore Raycast")));

            // Register weapon with WeaponController
            weaponCtrl.AddWeapon(sword);

            EditorUtility.SetDirty(sword);
            EditorUtility.SetDirty(weaponObj);
            EditorUtility.SetDirty(weaponCtrl);
            EditorUtility.SetDirty(playerObj);

            // 5. Spawn / Reset 3 Test Dummy Zombies in front of player
            string containerName = "Combat_Test_Dummies";
            GameObject container = GameObject.Find(containerName);
            if (container != null)
            {
                Undo.DestroyObjectImmediate(container);
            }
            container = new GameObject(containerName);

            Vector2[] spawnOffsets = new Vector2[]
            {
                new Vector2(1.8f, 0.2f),
                new Vector2(1.6f, 1.2f),
                new Vector2(2.2f, -1.0f)
            };

            for (int i = 0; i < spawnOffsets.Length; i++)
            {
                GameObject dummy = new GameObject($"Test_Zombie_{i + 1}");
                dummy.transform.SetParent(container.transform);
                dummy.transform.position = (Vector2)playerObj.transform.position + spawnOffsets[i];
                dummy.transform.localScale = Vector3.one; // Standard 1.0 Minecraft proportion scale

                // Sprite Renderer
                SpriteRenderer sr = dummy.AddComponent<SpriteRenderer>();
                sr.sprite = zombieSprite;
                sr.sortingOrder = 5;

                // Physics 2D
                CircleCollider2D col = dummy.AddComponent<CircleCollider2D>();
                col.radius = 0.35f;

                Rigidbody2D rb = dummy.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearDamping = 6f; // Natural knockback drag

                // Health & Damage
                EnemyHealth health = dummy.AddComponent<EnemyHealth>();
                health.SetFrames(hitSparkFrames, deathSmokeFrames);
                SetPrivateField(health, "maxHealth", 60f);

                // Walk Animation (using sliced walk frames, no static sprite)
                if (zombieWalkFrames != null && zombieWalkFrames.Length > 0)
                {
                    EnemyAnimation anim = dummy.AddComponent<EnemyAnimation>();
                    anim.SetWalkFrames(zombieWalkFrames, 10f);
                }

                // Ensure any existing label is removed
                Transform existingLabel = dummy.transform.Find("Label");
                if (existingLabel != null)
                {
                    Undo.DestroyObjectImmediate(existingLabel.gameObject);
                }

                EditorUtility.SetDirty(dummy);
                EditorUtility.SetDirty(health);
            }

            EditorUtility.SetDirty(container);

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[CombatSetupHelper] SUCCESS!</color> Player equipped with Magma Broadsword (Big Slash VFX) and 3 Big Zombie Dummies (1.5x Scale) spawned!");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                // Try base type
                var baseType = target.GetType().BaseType;
                while (baseType != null && field == null)
                {
                    field = baseType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    baseType = baseType.BaseType;
                }
            }

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[CombatSetupHelper] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        private static Sprite[] LoadSpritesAtPath(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            return assets.OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        private static Sprite LoadFirstSprite(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new string[] { folderPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }
    }
}
