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
        [MenuItem("PixelSurvivor/Setup Enemy Prefabs (All 10 Enemies)", false, 115)]
        public static void SetupEnemyPrefabs()
        {
            Debug.Log("<color=#38BDF8>[Enemy Setup]</color> Building all 10 Enemy Prefabs and wiring WaveSpawner...");

            string prefabsDir = "Assets/PixelSurvivor/Enemies/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor/Enemies", "Prefabs");
            }

            // 1. Load VFX Sprites
            Sprite[] hitSparks = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_hit_spark.png");
            Sprite[] deathSmoke = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_smoke_puff.png");
            Sprite[] shockwaveFire = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_shockwave_fire.png");
            Sprite[] shockwaveIce = LoadSprites("Assets/PixelSurvivor/Particles/VFX/vfx_shockwave_ice.png");
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

            // Batch 3 Sprites
            Sprite[] iceCreeperWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Ice_Creeper/ice_creeper_walk.png");
            Sprite[] iceCreeperAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Ice_Creeper/ice_creeper_attack.png");
            Sprite[] vexFly = LoadSprites("Assets/PixelSurvivor/Enemies/Vex/vex_fly.png");
            Sprite[] vexAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Vex/vex_attack.png");
            Sprite[] phantomFly = LoadSprites("Assets/PixelSurvivor/Enemies/Phantom/phantom_fly.png");
            Sprite[] phantomAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Phantom/phantom_attack.png");
            Sprite[] evokerWalk = LoadSprites("Assets/PixelSurvivor/Enemies/Evoker/evoker_walk.png");
            Sprite[] evokerAttack = LoadSprites("Assets/PixelSurvivor/Enemies/Evoker/evoker_attack.png");

            Debug.Log($"[Enemy Setup] Loaded Sprites: IceCreeperWalk({iceCreeperWalk.Length}), VexFly({vexFly.Length}), PhantomFly({phantomFly.Length}), EvokerWalk({evokerWalk.Length})");

            // 3. Create Prefabs (Vex first so Evoker can reference it)
            GameObject zombiePrefab = CreateZombiePrefab(prefabsDir, zombieWalk, hitSparks, deathSmoke);
            GameObject spiderPrefab = CreateSpiderPrefab(prefabsDir, spiderWalk, hitSparks, deathSmoke);
            GameObject creeperPrefab = CreateCreeperPrefab(prefabsDir, creeperWalk, creeperAttack, shockwaveFire, deathSmoke, hitSparks);
            GameObject slimePrefab = CreateSlimePrefab(prefabsDir, slimeMove, hitSparks, deathSmoke);
            GameObject witchPrefab = CreateWitchPrefab(prefabsDir, witchWalk, witchAttack, evokerSpikes, magicCircle, hitSparks, deathSmoke);
            GameObject huskPrefab = CreateHuskShovelPrefab(prefabsDir, huskShovelWalk, huskShovelAttack, hitSparks, deathSmoke);

            GameObject iceCreeperPrefab = CreateIceCreeperPrefab(prefabsDir, iceCreeperWalk, iceCreeperAttack, shockwaveIce, deathSmoke, hitSparks);
            GameObject vexPrefab = CreateVexPrefab(prefabsDir, vexFly, vexAttack, hitSparks, deathSmoke);
            GameObject phantomPrefab = CreatePhantomPrefab(prefabsDir, phantomFly, phantomAttack, hitSparks, deathSmoke);
            GameObject evokerPrefab = CreateEvokerPrefab(prefabsDir, evokerWalk, evokerAttack, vexPrefab, magicCircle, deathSmoke, hitSparks);

            AssetDatabase.SaveAssets();

            // 4. Wire templates to WaveSpawner in Active Scene & MainGame & CombatSandbox
            LinkSceneTemplates("Assets/Scenes/MainGame.unity", zombiePrefab, spiderPrefab, creeperPrefab, slimePrefab, witchPrefab, huskPrefab, iceCreeperPrefab, vexPrefab, phantomPrefab, evokerPrefab);
            LinkSceneTemplates("Assets/Scenes/CombatSandbox.unity", zombiePrefab, spiderPrefab, creeperPrefab, slimePrefab, witchPrefab, huskPrefab, iceCreeperPrefab, vexPrefab, phantomPrefab, evokerPrefab);

            Debug.Log("<color=#10B981>[Enemy Setup]</color> All 10 Enemy Prefabs created and wired successfully!");
        }

        private static void LinkSceneTemplates(string scenePath, GameObject z, GameObject s, GameObject c, GameObject slime, GameObject w, GameObject h, GameObject iceC, GameObject vex, GameObject phantom, GameObject evoker)
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
                soSpawner.FindProperty("iceCreeperTemplate").objectReferenceValue = iceC;
                soSpawner.FindProperty("vexTemplate").objectReferenceValue = vex;
                soSpawner.FindProperty("phantomTemplate").objectReferenceValue = phantom;
                soSpawner.FindProperty("evokerTemplate").objectReferenceValue = evoker;
                soSpawner.ApplyModifiedProperties();

                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=#10B981>[Enemy Setup]</color> Linked 10 enemy templates to WaveSpawner in {scenePath}");
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
            col.radius = 0.30f;
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
            soAi.FindProperty("separationRadius").floatValue = 0.40f;
            soAi.FindProperty("separationForce").floatValue = 1.5f;
            soAi.FindProperty("animationFps").floatValue = 16f;
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
            soAi.FindProperty("contactDamage").floatValue = 5f;
            soAi.FindProperty("separationRadius").floatValue = 0.50f;
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
            soEh.FindProperty("maxHealth").floatValue = 60f; // Slime HP >= Zombie
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

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
            soAi.FindProperty("contactDamage").floatValue = 6f;
            soAi.FindProperty("preferredMinDistance").floatValue = 4.2f;
            soAi.FindProperty("preferredMaxDistance").floatValue = 6.5f;
            soAi.FindProperty("attackCooldown").floatValue = 3.8f;
            soAi.FindProperty("spikeDamage").floatValue = 20f;
            soAi.FindProperty("spikeRadius").floatValue = 1.15f;
            soAi.FindProperty("castFps").floatValue = 8f; // ~1.75s windup
            soAi.FindProperty("telegraphDelay").floatValue = 0.85f;
            soAi.FindProperty("animationFps").floatValue = 10f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            AssignSpriteArray(soAi.FindProperty("spikeVfxFrames"), spikeFrames);
            AssignSpriteArray(soAi.FindProperty("magicCircleFrames"), circleFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
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
            return prefab;
        }

        private static GameObject CreateIceCreeperPrefab(string dir, Sprite[] walkFrames, Sprite[] attackFrames, Sprite[] shockwaveFrames, Sprite[] smokeFrames, Sprite[] sparks)
        {
            string path = $"{dir}/Enemy_IceCreeper.prefab";
            GameObject go = new GameObject("Enemy_IceCreeper");

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
            soEh.FindProperty("maxHealth").floatValue = 65f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smokeFrames);
            soEh.ApplyModifiedProperties();

            IceCreeperAI ai = go.AddComponent<IceCreeperAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 3.0f;
            soAi.FindProperty("contactDamage").floatValue = 5f;
            soAi.FindProperty("separationRadius").floatValue = 0.50f;
            soAi.FindProperty("separationForce").floatValue = 1.2f;
            soAi.FindProperty("animationFps").floatValue = 10f;
            soAi.FindProperty("triggerDistance").floatValue = 1.6f;
            soAi.FindProperty("fuseDuration").floatValue = 1.2f;
            soAi.FindProperty("explosionRadius").floatValue = 2.6f;
            soAi.FindProperty("explosionDamage").floatValue = 35f;
            soAi.FindProperty("mobDamage").floatValue = 55f;
            soAi.FindProperty("slowMultiplier").floatValue = 0.55f;
            soAi.FindProperty("slowDuration").floatValue = 1.8f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            AssignSpriteArray(soAi.FindProperty("shockwaveFrames"), shockwaveFrames);
            AssignSpriteArray(soAi.FindProperty("smokeFrames"), smokeFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Ice Creeper Prefab at {path}");
            return prefab;
        }

        private static GameObject CreateVexPrefab(string dir, Sprite[] flyFrames, Sprite[] attackFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Vex.prefab";
            GameObject go = new GameObject("Enemy_Vex");
            go.transform.localScale = new Vector3(0.65f, 0.65f, 1f); // Small flying minion

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 25f; // Fragile minion
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = flyFrames != null && flyFrames.Length > 0 ? flyFrames[0] : null;
            sr.sortingOrder = 7; // Flying above ground mobs

            VexAI ai = go.AddComponent<VexAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 4.2f;
            soAi.FindProperty("contactDamage").floatValue = 8f;
            soAi.FindProperty("attackTriggerDistance").floatValue = 1.2f;
            soAi.FindProperty("attackSwoopSpeed").floatValue = 5.5f;
            soAi.FindProperty("attackCooldown").floatValue = 2.0f;
            soAi.FindProperty("attackFps").floatValue = 16f;
            soAi.FindProperty("animationFps").floatValue = 12f;
            soAi.FindProperty("spriteRenderer").objectReferenceValue = sr;
            AssignSpriteArray(soAi.FindProperty("flyFrames"), flyFrames);
            AssignSpriteArray(soAi.FindProperty("walkFrames"), flyFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Vex Prefab at {path}");
            return prefab;
        }

        private static GameObject CreatePhantomPrefab(string dir, Sprite[] flyFrames, Sprite[] attackFrames, Sprite[] sparks, Sprite[] smoke)
        {
            string path = $"{dir}/Enemy_Phantom.prefab";
            GameObject go = new GameObject("Enemy_Phantom");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = flyFrames != null && flyFrames.Length > 0 ? flyFrames[0] : null;
            sr.sortingOrder = 8; // Airborne hunter

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.40f;
            col.isTrigger = true;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            EnemyHealth eh = go.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 35f;
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            PhantomAI ai = go.AddComponent<PhantomAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 4.0f; // Cruise speed
            soAi.FindProperty("contactDamage").floatValue = 8f;
            soAi.FindProperty("prowlDistanceMin").floatValue = 3.5f;
            soAi.FindProperty("prowlDistanceMax").floatValue = 5.5f;
            soAi.FindProperty("diveSpeed").floatValue = 6.5f;
            soAi.FindProperty("diveCooldown").floatValue = 3.8f;
            soAi.FindProperty("diveDuration").floatValue = 1.0f;
            soAi.FindProperty("pullUpDuration").floatValue = 0.6f;
            soAi.FindProperty("diveDamage").floatValue = 14f;
            soAi.FindProperty("attackFps").floatValue = 16f;
            soAi.FindProperty("animationFps").floatValue = 11f;
            soAi.FindProperty("spriteRenderer").objectReferenceValue = sr;
            AssignSpriteArray(soAi.FindProperty("flyFrames"), flyFrames);
            AssignSpriteArray(soAi.FindProperty("walkFrames"), flyFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Phantom Prefab at {path}");
            return prefab;
        }

        private static GameObject CreateEvokerPrefab(string dir, Sprite[] walkFrames, Sprite[] attackFrames, GameObject vexPrefabRef, Sprite[] magicCircleFrames, Sprite[] smoke, Sprite[] sparks)
        {
            string path = $"{dir}/Enemy_Evoker.prefab";
            GameObject go = new GameObject("Enemy_Evoker");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null;
            sr.sortingOrder = 6;

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
            soEh.FindProperty("maxHealth").floatValue = 150f; // Elite summoner
            soEh.FindProperty("isInvincible").boolValue = false;
            AssignSpriteArray(soEh.FindProperty("hitSparkFrames"), sparks);
            AssignSpriteArray(soEh.FindProperty("deathSmokeFrames"), smoke);
            soEh.ApplyModifiedProperties();

            EvokerAI ai = go.AddComponent<EvokerAI>();
            SerializedObject soAi = new SerializedObject(ai);
            soAi.FindProperty("moveSpeed").floatValue = 1.8f;
            soAi.FindProperty("contactDamage").floatValue = 10f;
            soAi.FindProperty("separationRadius").floatValue = 0.60f;
            soAi.FindProperty("separationForce").floatValue = 1.2f;
            soAi.FindProperty("summonCooldown").floatValue = 6.0f;
            soAi.FindProperty("minVexToSummon").intValue = 2;
            soAi.FindProperty("maxVexToSummon").intValue = 3;
            soAi.FindProperty("summonAnimationFps").floatValue = 10f;
            soAi.FindProperty("vexPrefab").objectReferenceValue = vexPrefabRef;
            soAi.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soAi.FindProperty("animationFps").floatValue = 10f;
            AssignSpriteArray(soAi.FindProperty("walkFrames"), walkFrames);
            AssignSpriteArray(soAi.FindProperty("attackFrames"), attackFrames);
            AssignSpriteArray(soAi.FindProperty("magicCircleFrames"), magicCircleFrames);
            AssignSpriteArray(soAi.FindProperty("summonSmokeFrames"), smoke);
            soAi.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"<color=#10B981>[Enemy Setup]</color> Created Evoker Prefab at {path}");
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
