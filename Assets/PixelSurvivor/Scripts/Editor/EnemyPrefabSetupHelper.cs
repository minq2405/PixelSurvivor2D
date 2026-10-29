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
        [MenuItem("PixelSurvivor/Setup Enemy Prefabs (Zombie, Spider, Creeper, Slime, Witch, Husk)", false, 115)]
        public static void SetupEnemyPrefabs()
        {
            Debug.Log("<color=#38BDF8>[Enemy Setup]</color> Building all 6 Enemy Prefabs and wiring WaveSpawner...");

            string prefabsDir = "Assets/PixelSurvivor/Enemies/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor/Enemies", "Prefabs");
            }

            // 1. Load VFX Sprites
            Sprite[] hitSparks = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_hit_spark.png");
            Sprite[] deathSmoke = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_smoke_puff.png");
            Sprite[] shockwave = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_shockwave_fire.png");
            Sprite[] evokerSpikes = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_evoker_skill.png");
            Sprite[] magicCircle = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_evoker_magic_circle.png");

            // 2. Load Enemy Sprites
            Sprite[] zombieWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Zombie/zombie_walk.png");
            Sprite[] spiderWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Spider/spider_walk.png");
            Sprite[] creeperWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Creeper/creeper_walk.png");
            Sprite[] creeperAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Creeper/creeper_attack.png");
            Sprite[] slimeMove = LoadSprites("Assets/PixelSurvivor/Enemies/Slime/slime_move.png");
            Sprite[] witchWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Witch/witch_walk.png");
            Sprite[] witchAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Witch/witch_attack.png");
            Sprite[] huskShovelWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Husk/husk_shovel_walk.png");
            Sprite[] huskShovelAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Husk/husk_shovel_attack.png");

            Debug.Log($"[Enemy Setup] Loaded Sprites: Slime({slimeMove.Length}), WitchWalk({witchWalk.Length}), WitchAtk({witchAttack.Length}), HuskWalk({huskShovelWalk.Length}), HuskAtk({huskShovelAttack.Length}), Spikes({evokerSpikes.Length}), Circle({magicCircle.Length})");

            // 3. Create Prefabs
            GameObject zombiePrefab = CreateZombiePrefab(prefabsDir, zombieWalk, hitSparks, deathSmoke);
            GameObject spiderPrefab = CreateSpiderPrefab(prefabsDir, spiderWalk, hitSparks, deathSmoke);
            GameObject creeperPrefab = CreateCreeperPrefab(prefabsDir, creeperWalk, creeperAttack, shockwave, deathSmoke, hitSparks);
            GameObject slimePrefab = CreateSlimePrefab(prefabsDir, slimeMove, hitSparks, deathSmoke);
            GameObject witchPrefab = CreateWitchPrefab(prefabsDir, witchWalk, witchAttack, evokerSpikes, magicCircle, hitSparks, deathSmoke);
            GameObject huskPrefab = CreateHuskShovelPrefab(prefabsDir, huskShovelWalk, huskShovelAttack, hitSparks, deathSmoke);

            AssetDatabase.SaveAssets();

            // 4. Wire templates to WaveSpawner in Active Scene & MainGame & CombatSandbox
            LinkSceneTemplates("Assets/Scenes/MainGame.unity", zombiePrefab, spiderPrefab, creeperPrefab, slimePrefab, witchPrefab, huskPrefab);
            LinkSceneTemplates("Assets/Scenes/CombatSandbox.unity", zombiePrefab, spiderPrefab, creeperPrefab, slimePrefab, witchPrefab, huskPrefab);

            Debug.Log("<color=#10B981>[Enemy Setup]</color> All 6 Enemy Prefabs created and wired successfully!");
        }

        private static void LinkSceneTemplates(string scenePath, GameObject z, GameObject s, GameObject c, GameObject slime, GameObject w, GameObject h)
        {
            if (!File.Exists(scenePath)) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveSpawner spawner = Object.FindAnyObjectByType<WaveSpawner>();
            if (spawner != null)
            {
                SerializedObject soSpawner = new SerializedObject(spawner);
                soSpawner.FindProperty("zombieTemplate").objectReferenceValue = z;
                soSpawner.FindProperty("spiderTemplate").objectReferenceValue = s;
                soSpawner.FindProperty("creeperTemplate").objectReferenceValue = c;
                soSpawner.FindProperty("slimeTemplate").objectReferenceValue = slime;
                soSpawner.FindProperty("witchTemplate").objectReferenceValue = w;
                soSpawner.FindProperty("huskShovelTemplate").objectReferenceValue = h;
                soSpawner.ApplyModifiedProperties();

                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=#10B981>[Enemy Setup]</color> Linked 6 enemy templates to WaveSpawner in {scenePath}");
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
            soAi.FindProperty("moveSpeed").floatValue = 4.2f;
            soAi.FindProperty("contactDamage").floatValue = 8f;
            soAi.FindProperty("separationRadius").floatValue = 0.50f;
            soAi.FindProperty("separationForce").floatValue = 1.5f;
            soAi.FindProperty("animationFps").floatValue = 14f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
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
            return prefab;
        }

        private static GameObject CreateSlimePrefab(string dir, Sprite[] moveFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Slime.prefab";
            GameObject go = new GameObject("Enemy_Slime");

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
            soEh.FindProperty("maxHealth").floatValue = 60f; // Slime HP >= Zombie per user instruction
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            // Create Visual child so visual hop height is decoupled from ground collider
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = moveFrames != null && moveFrames.Length > 0 ? moveFrames[0] : null;
            sr.sortingOrder = 5;

            SlimeAI ai = go.AddComponent<SlimeAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("jumpSpeedX").floatValue = 5.4f;
            soAi.FindProperty("jumpSpeedY").floatValue = 2.6f;
            soAi.FindProperty("jumpArcHeight").floatValue = 0.55f;
            soAi.FindProperty("visualTransform").objectReferenceValue = visual.transform;
            soAi.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soAi.FindProperty("contactDamage").floatValue = 10f;
            soAi.FindProperty("splitHealth").floatValue = 25f;
            soAi.FindProperty("isSmallSlime").boolValue = false;
            soAi.FindProperty("animationFps").floatValue = 12f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), moveFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Slime Prefab with Parabolic Hop at {path}");
            return prefab;
        }

        private static GameObject CreateWitchPrefab(string dir, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] spikeFrames, Sprite[] circleFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Witch.prefab";
            GameObject go = new GameObject("Enemy_Witch");

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
            soEh.FindProperty("maxHealth").floatValue = 40f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            WitchAI ai = go.AddComponent<WitchAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 2.2f;
            soAi.FindProperty("preferredMinDistance").floatValue = 4.2f;
            soAi.FindProperty("preferredMaxDistance").floatValue = 6.5f;
            soAi.FindProperty("attackCooldown").floatValue = 3.8f;
            soAi.FindProperty("spikeDamage").floatValue = 20f;
            soAi.FindProperty("spikeRadius").floatValue = 1.15f;
            soAi.FindProperty("castFps").floatValue = 8f; // Lengthened windup animation (~1.75s)
            soAi.FindProperty("telegraphDelay").floatValue = 0.85f; // Fair 0.85s telegraph window to dodge
            soAi.FindProperty("animationFps").floatValue = 10f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            AssignSpriteArray(soAi.FindProperty("spikeVfxFrames"), spikeFrames);
            AssignSpriteArray(soAi.FindProperty("magicCircleFrames"), circleFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Witch Prefab with Amethyst Telegraph at {path}");
            return prefab;
        }

        private static GameObject CreateHuskShovelPrefab(string dir, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_HuskShovel.prefab";
            GameObject go = new GameObject("Enemy_HuskShovel");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.38f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 85f; // Heavy tank
            soEh.FindProperty("knockbackResistance").floatValue = 0.5f; // 50% resistance
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            HuskShovelAI ai = go.AddComponent<HuskShovelAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 1.6f;
            soAi.FindProperty("attackRange").floatValue = 1.35f;
            soAi.FindProperty("attackCooldown").floatValue = 1.8f;
            soAi.FindProperty("swingDamage").floatValue = 18f;
            soAi.FindProperty("swingRadius").floatValue = 1.1f;
            soAi.FindProperty("swingKnockback").floatValue = 8.0f;
            soAi.FindProperty("animationFps").floatValue = 10f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Husk Shovel Prefab at {path}");
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
