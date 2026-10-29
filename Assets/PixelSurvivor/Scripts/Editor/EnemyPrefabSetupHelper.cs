using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PixelSurvivor.Combat;

namespace PixelSurvivor.Editor
{
    public static class EnemyPrefabSetupHelper
    {
        [MenuItem("PixelSurvivor/Setup Enemy Prefabs (Zombie, Spider, Creeper)", false, 115)]
        public static void SetupEnemyPrefabs()
        {
            Debug.Log("<color=#38BDF8>[Enemy Setup]</color> Building Enemy Prefabs and wiring WaveSpawner...");

            string prefabsDir = "Assets/PixelSurvivor/Enemies/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor/Enemies", "Prefabs");
            }

            // 1. Load VFX Sprites
            Sprite[] hitSparks = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_hit_spark.png");
            Sprite[] deathSmoke = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_smoke_puff.png");
            Sprite[] shockwave = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_shockwave_fire.png");

            // 2. Load Enemy Sprites
            Sprite[] zombieWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Zombie/zombie_walk.png");
            Sprite[] spiderWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Spider/spider_walk.png");
            Sprite[] creeperWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Creeper/creeper_walk.png");
            Sprite[] creeperAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Creeper/creeper_attack.png");

            Debug.Log($"[Enemy Setup] Loaded: Zombie({zombieWalk.Length}), Spider({spiderWalk.Length}), CreeperWalk({creeperWalk.Length}), CreeperAtk({creeperAttack.Length}), Shockwave({shockwave.Length}), Smoke({deathSmoke.Length})");

            // 3. Create Zombie Prefab
            GameObject zombiePrefab = CreateZombiePrefab(prefabsDir, zombieWalk, hitSparks, deathSmoke);

            // 4. Create Spider Prefab
            GameObject spiderPrefab = CreateSpiderPrefab(prefabsDir, spiderWalk, hitSparks, deathSmoke);

            // 5. Create Creeper Prefab
            GameObject creeperPrefab = CreateCreeperPrefab(prefabsDir, creeperWalk, creeperAttack, shockwave, deathSmoke, hitSparks);

            AssetDatabase.SaveAssets();

            // 6. Connect Templates to WaveSpawner on [Game_Manager] in Active Scene
            WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>();
            if (spawner != null)
            {
                SerializedObject soSpawner = new SerializedObject(spawner);
                soSpawner.FindProperty("zombieTemplate").objectReferenceValue = zombiePrefab;
                soSpawner.FindProperty("spiderTemplate").objectReferenceValue = spiderPrefab;
                soSpawner.FindProperty("creeperTemplate").objectReferenceValue = creeperPrefab;
                soSpawner.ApplyModifiedProperties();

                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                EditorSceneManager.SaveScene(spawner.gameObject.scene);
                Debug.Log("<color=#10B981>[Enemy Setup]</color> WaveSpawner successfully linked to all 3 enemy prefabs and scene saved!");
            }
            else
            {
                Debug.LogWarning("[Enemy Setup] WaveSpawner not found in current scene!");
            }
        }

        private static GameObject CreateZombiePrefab(string dir, Sprite[] walkFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Zombie.prefab";
            GameObject go = new GameObject("Enemy_Zombie");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 50f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            EnemyAI ai = go.AddComponent<EnemyAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 1.8f;
            soAi.FindProperty("contactDamage").floatValue = 6f;
            soAi.FindProperty("separationRadius").floatValue = 0.55f;
            soAi.FindProperty("separationForce").floatValue = 1.2f;
            soAi.FindProperty("animationFps").floatValue = 10f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Zombie Prefab at {path}");
            return prefab;
        }

        private static GameObject CreateSpiderPrefab(string dir, Sprite[] walkFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Spider.prefab";
            GameObject go = new GameObject("Enemy_Spider");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 30f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            EnemyAI ai = go.AddComponent<EnemyAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 4.2f; // Fast flanker
            soAi.FindProperty("contactDamage").floatValue = 8f;
            soAi.FindProperty("separationRadius").floatValue = 0.50f;
            soAi.FindProperty("separationForce").floatValue = 1.5f;
            soAi.FindProperty("animationFps").floatValue = 14f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Spider Prefab at {path}");
            return prefab;
        }

        private static GameObject CreateCreeperPrefab(string dir, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] shockwaveFrames, Sprite[] smokeFrames, Sprite[] sparks)
        {
            string path = $"{dir}/Enemy_Creeper.prefab";
            GameObject go = new GameObject("Enemy_Creeper");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 60f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smokeFrames);
            soEh.ApplyModifiedProperties();

            CreeperAI ai = go.AddComponent<CreeperAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 3.2f;
            soAi.FindProperty("contactDamage").floatValue = 10f;
            soAi.FindProperty("separationRadius").floatValue = 0.55f;
            soAi.FindProperty("separationForce").floatValue = 1.2f;
            soAi.FindProperty("animationFps").floatValue = 10f;
            soAi.FindProperty("triggerDistance").floatValue = 1.6f;
            soAi.FindProperty("fuseDuration").floatValue = 1.2f;
            soAi.FindProperty("explosionRadius").floatValue = 2.6f;
            soAi.FindProperty("explosionDamage").floatValue = 45f;
            soAi.FindProperty("mobDamage").floatValue = 55f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            AssignSpriteArray(soAi.FindProperty("shockwaveFrames"), shockwaveFrames);
            AssignSpriteArray(soAi.FindProperty("smokeFrames"), smokeFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Creeper Prefab at {path}");
            return prefab;
        }

        private static void AssignSpriteArray(SerializedProperty prop, Sprite[] sprites)
        {
            if (prop == null || sprites == null) return;
            prop.ClearArray();
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
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
