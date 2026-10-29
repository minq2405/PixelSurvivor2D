using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.UI;
using PixelSurvivor.Combat;

namespace PixelSurvivor.Editor
{
    public static class ExpSystemSetupHelper
    {
        [MenuItem("PixelSurvivor/Setup EXP & Drop System", false, 110)]
        public static void SetupExpAndDropSystem()
        {
            Debug.Log("<color=#38BDF8>[EXP Setup]</color> Beginning EXP & Drop System configuration...");

            // 1. Ensure Directory
            string prefabsDir = "Assets/PixelSurvivor/Items/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/PixelSurvivor/Items", "Prefabs");
            }

            // 2. Load Sprites
            Sprite spExpGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_exp_orb_green.png");
            Sprite spExpYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_exp_orb_yellow.png");
            Sprite spEmerald = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Passives/item_emerald_exp.png");
            Sprite spLuckyBlock = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PixelSurvivor/Items/Drops/drop_lucky_block_yellow.png");

            // 3. Create or Update Prefabs with Enlarged Scale & Adjusted Colliders
            GameObject expGreenPrefab = CreateOrUpdateDropPrefab(prefabsDir + "/Drop_ExpOrb_Green.prefab", spExpGreen, PickupType.ExpSmall, 10, 1.25f, 0.45f);
            GameObject expYellowPrefab = CreateOrUpdateDropPrefab(prefabsDir + "/Drop_ExpOrb_Yellow.prefab", spExpYellow, PickupType.ExpMedium, 50, 1.45f, 0.50f);
            GameObject emeraldPrefab = CreateOrUpdateDropPrefab(prefabsDir + "/Drop_Emerald.prefab", spEmerald, PickupType.Emerald, 1, 1.30f, 0.45f);
            GameObject luckyBlockPrefab = CreateOrUpdateDropPrefab(prefabsDir + "/Drop_LuckyBlock.prefab", spLuckyBlock, PickupType.ExpMedium, 50, 1.35f, 0.50f);

            AssetDatabase.SaveAssets();

            // 4. Setup PlayerCollector on Player in Scene (Reduced radius by ~1/3 to 1.7m)
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                PlayerCollector collector = playerObj.GetComponent<PlayerCollector>();
                if (collector == null)
                {
                    collector = Undo.AddComponent<PlayerCollector>(playerObj);
                    Debug.Log("<color=#10B981>[EXP Setup]</color> Added PlayerCollector component to Player.");
                }

                SerializedObject soCollector = new SerializedObject(collector);
                soCollector.FindProperty("baseRadius").floatValue = 1.7f;
                soCollector.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[EXP Setup] Player GameObject not found in active scene!");
            }

            // 5. Setup ExperienceManager on [Game_Manager]
            GameObject gm = GameObject.Find("[Game_Manager]");
            if (gm != null)
            {
                ExperienceManager expMgr = gm.GetComponent<ExperienceManager>();
                if (expMgr == null)
                {
                    expMgr = Undo.AddComponent<ExperienceManager>(gm);
                    Debug.Log("<color=#10B981>[EXP Setup]</color> Added ExperienceManager component to [Game_Manager].");
                }

                SerializedObject soExp = new SerializedObject(expMgr);
                soExp.FindProperty("expOrbGreenPrefab").objectReferenceValue = expGreenPrefab;
                soExp.FindProperty("expOrbYellowPrefab").objectReferenceValue = expYellowPrefab;
                soExp.FindProperty("emeraldPrefab").objectReferenceValue = emeraldPrefab;
                soExp.FindProperty("luckyBlockPrefab").objectReferenceValue = luckyBlockPrefab;
                soExp.ApplyModifiedProperties();
            }

            // 6. Configure EXP_Bar_Container to stretch across top, initially dark background, fill with Cyan
            ConfigureExpBarLayout();

            // 7. Update Character_Name and Level_Text default texts
            UpdatePlayerStatsText();

            // 8. Connect InGameHUD references
            InGameHUD hud = Object.FindAnyObjectByType<InGameHUD>();
            if (hud != null)
            {
                SerializedObject soHud = new SerializedObject(hud);
                Transform expFill = GameObject.Find("InGame_Canvas/HUD_Root/EXP_Bar_Container/Background/Fill")?.transform;
                if (expFill != null)
                {
                    soHud.FindProperty("expFillImage").objectReferenceValue = expFill.GetComponent<Image>();
                    soHud.FindProperty("expFillRect").objectReferenceValue = expFill.GetComponent<RectTransform>();
                }
                Transform charName = GameObject.Find("InGame_Canvas/HUD_Root/Player_Stats/Character_Name")?.transform;
                if (charName != null)
                {
                    soHud.FindProperty("characterNameText").objectReferenceValue = charName.GetComponent<TMP_Text>();
                }
                Transform lvlText = GameObject.Find("InGame_Canvas/HUD_Root/Player_Stats/Level_Text")?.transform;
                if (lvlText != null)
                {
                    soHud.FindProperty("levelText").objectReferenceValue = lvlText.GetComponent<TMP_Text>();
                }
                soHud.ApplyModifiedProperties();
            }

            // 9. Mark Active Scene Dirty and Save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#10B981>[EXP Setup]</color> EXP & Drop System successfully configured and saved!");
        }

        private static void ConfigureExpBarLayout()
        {
            GameObject expBarObj = GameObject.Find("InGame_Canvas/HUD_Root/EXP_Bar_Container");
            if (expBarObj == null) return;

            Undo.RecordObject(expBarObj, "Configure EXP Bar Container");
            RectTransform rtContainer = expBarObj.GetComponent<RectTransform>();
            if (rtContainer != null)
            {
                // Full top-stretch anchor
                rtContainer.anchorMin = new Vector2(0f, 1f);
                rtContainer.anchorMax = new Vector2(1f, 1f);
                rtContainer.pivot = new Vector2(0.5f, 1f);
                rtContainer.anchoredPosition = new Vector2(0f, 0f);
                rtContainer.sizeDelta = new Vector2(0f, 16f);
                rtContainer.offsetMin = new Vector2(0f, -16f);
                rtContainer.offsetMax = new Vector2(0f, 0f);
            }

            // Background
            Transform bgTrans = expBarObj.transform.Find("Background");
            if (bgTrans != null)
            {
                Undo.RecordObject(bgTrans, "Configure EXP Background");
                RectTransform rtBg = bgTrans.GetComponent<RectTransform>();
                if (rtBg != null)
                {
                    rtBg.anchorMin = Vector2.zero;
                    rtBg.anchorMax = Vector2.one;
                    rtBg.offsetMin = Vector2.zero;
                    rtBg.offsetMax = Vector2.zero;
                }
                Image imgBg = bgTrans.GetComponent<Image>();
                if (imgBg != null)
                {
                    imgBg.color = new Color(0.043f, 0.059f, 0.098f, 0.95f); // Dark Obsidian
                }
            }

            // Fill (starts empty / 0% width, showing dark background)
            Transform fillTrans = expBarObj.transform.Find("Background/Fill");
            if (fillTrans != null)
            {
                Undo.RecordObject(fillTrans, "Configure EXP Fill");
                RectTransform rtFill = fillTrans.GetComponent<RectTransform>();
                if (rtFill != null)
                {
                    rtFill.anchorMin = new Vector2(0f, 0f);
                    rtFill.anchorMax = new Vector2(0f, 1f); // 0 width initially
                    rtFill.offsetMin = Vector2.zero;
                    rtFill.offsetMax = Vector2.zero;
                }
                Image imgFill = fillTrans.GetComponent<Image>();
                if (imgFill != null)
                {
                    imgFill.color = new Color(0.220f, 0.741f, 0.973f, 1f); // Diamond Cyan
                }
            }

            // Border (Hairline subtle bottom border)
            Transform borderTrans = expBarObj.transform.Find("Border");
            if (borderTrans != null)
            {
                Undo.RecordObject(borderTrans, "Configure EXP Border");
                RectTransform rtBorder = borderTrans.GetComponent<RectTransform>();
                if (rtBorder != null)
                {
                    rtBorder.anchorMin = new Vector2(0f, 0f);
                    rtBorder.anchorMax = new Vector2(1f, 0f);
                    rtBorder.pivot = new Vector2(0.5f, 0f);
                    rtBorder.anchoredPosition = new Vector2(0f, 0f);
                    rtBorder.sizeDelta = new Vector2(0f, 2f);
                }
                Image imgBorder = borderTrans.GetComponent<Image>();
                if (imgBorder != null)
                {
                    imgBorder.color = new Color(0.200f, 0.255f, 0.333f, 0.85f); // Subtle Slate
                }
            }
        }

        private static void UpdatePlayerStatsText()
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            string pName = player != null && !string.IsNullOrEmpty(player.characterName) ? player.characterName : "Steve";

            TMP_Text nameTmp = GameObject.Find("InGame_Canvas/HUD_Root/Player_Stats/Character_Name")?.GetComponent<TMP_Text>();
            if (nameTmp != null)
            {
                Undo.RecordObject(nameTmp, "Update Character Name Text");
                nameTmp.text = pName;
            }

            TMP_Text lvlTmp = GameObject.Find("InGame_Canvas/HUD_Root/Player_Stats/Level_Text")?.GetComponent<TMP_Text>();
            if (lvlTmp != null)
            {
                Undo.RecordObject(lvlTmp, "Update Level Text");
                lvlTmp.text = "Lv.1";
            }
        }

        private static GameObject CreateOrUpdateDropPrefab(string assetPath, Sprite sprite, PickupType type, int value, float scale, float colRadius)
        {
            GameObject tempObj = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            tempObj.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            CircleCollider2D col = tempObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = colRadius;

            PickupItem pickup = tempObj.AddComponent<PickupItem>();
            pickup.Initialize(type, value);

            SerializedObject soPickup = new SerializedObject(pickup);
            soPickup.FindProperty("itemType").enumValueIndex = (int)type;
            soPickup.FindProperty("value").intValue = value;
            soPickup.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, assetPath);
            Object.DestroyImmediate(tempObj);

            Debug.Log($"<color=#38BDF8>[EXP Setup]</color> Updated prefab: {assetPath} (scale={scale}, radius={colRadius})");
            return prefab;
        }
    }
}
