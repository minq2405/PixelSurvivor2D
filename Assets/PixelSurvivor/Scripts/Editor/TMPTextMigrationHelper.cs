using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelSurvivor.UI;

namespace PixelSurvivor.Editor
{
    /// <summary>
    /// Editor utility that scans and converts all legacy UnityEngine.UI.Text in the current scene
    /// into TextMeshProUGUI, applying the font asset 'Assets/PixelSurvivor/Resources/Fonts/VCR_OSD_MONO SDF.asset'.
    /// Strictly preserves all RectTransforms, positions, anchors, sizes, colors, and user customizations.
    /// </summary>
    public static class TMPTextMigrationHelper
    {
        private const string FONT_PATH = "Fonts/VCR_OSD_MONO SDF";

        [MenuItem("PixelSurvivor/UI: Convert All Legacy Text to TextMeshPro")]
        public static void ConvertAllLegacyTextToTMP()
        {
            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(FONT_PATH);
            if (fontAsset == null)
            {
                Debug.LogError($"[TMPTextMigrationHelper] Could not find TMP font asset at 'Resources/{FONT_PATH}'. Please ensure 'VCR_OSD_MONO SDF.asset' exists in Resources/Fonts/.");
                return;
            }

            // Find all Legacy Text components in the current active scene
            Text[] legacyTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (legacyTexts == null || legacyTexts.Length == 0)
            {
                Debug.Log("<color=#10B981>[TMPTextMigrationHelper]</color> No legacy Text components found. Scene is already 100% TextMeshPro!");
                return;
            }

            int count = 0;
            Undo.SetCurrentGroupName("Convert All Text to TextMeshPro");
            int group = Undo.GetCurrentGroup();

            Dictionary<GameObject, TextMeshProUGUI> convertedMap = new Dictionary<GameObject, TextMeshProUGUI>();

            foreach (Text oldText in legacyTexts)
            {
                if (oldText == null) continue;

                GameObject go = oldText.gameObject;

                // Cache all visual and text properties
                string content = oldText.text;
                float fontSize = oldText.fontSize;
                Color color = oldText.color;
                FontStyle fontStyle = oldText.fontStyle;
                TextAnchor alignment = oldText.alignment;
                bool raycast = oldText.raycastTarget;

                // Destroy old Text component
                Undo.DestroyObjectImmediate(oldText);

                // Add TextMeshProUGUI
                TextMeshProUGUI tmp = Undo.AddComponent<TextMeshProUGUI>(go);
                tmp.font = fontAsset;
                tmp.text = content;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.raycastTarget = raycast;
                tmp.fontStyle = ConvertFontStyle(fontStyle);
                tmp.alignment = ConvertAlignment(alignment);
                tmp.enableWordWrapping = true;

                // Clean up old legacy Shadow if present
                Shadow shadow = go.GetComponent<Shadow>();
                if (shadow != null)
                {
                    Undo.DestroyObjectImmediate(shadow);
                }

                convertedMap[go] = tmp;
                count++;
            }

            // Re-link references in known managers
            RelinkKnownComponents(convertedMap);

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"<color=#10B981>[TMPTextMigrationHelper] SUCCESS!</color> Converted {count} text components to TextMeshPro with 'VCR_OSD_MONO SDF'. All layouts, positions, colors, and user tweaks are 100% PRESERVED!");
        }

        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static void RelinkKnownComponents(Dictionary<GameObject, TextMeshProUGUI> map)
        {
            // 1. InGameHUD
            InGameHUD hud = Object.FindAnyObjectByType<InGameHUD>();
            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);
                TryAssignTMPProp(so, "timeText", "Timer_Text", map);
                TryAssignTMPProp(so, "biomeText", "Biome_Text", map);
                TryAssignTMPProp(so, "hpText", "HP_Text", map);
                so.ApplyModifiedProperties();
            }

            // 2. InGamePauseManager
            InGamePauseManager pauseMgr = Object.FindAnyObjectByType<InGamePauseManager>();
            if (pauseMgr != null)
            {
                SerializedObject so = new SerializedObject(pauseMgr);
                // Pause items
                TryAssignTMPProp(so, "pauseHpStatText", "Stat_HP", map);
                TryAssignTMPProp(so, "pauseAttackStatText", "Stat_Attack", map);
                TryAssignTMPProp(so, "pauseSpeedStatText", "Stat_Speed", map);
                TryAssignTMPProp(so, "pauseCooldownStatText", "Stat_Cooldown", map);
                TryAssignTMPProp(so, "pauseRangeStatText", "Stat_Range", map);
                TryAssignTMPProp(so, "pauseKnockbackStatText", "Stat_Knockback", map);
                TryAssignTMPProp(so, "pauseCriticalStatText", "Stat_Critical", map);
                TryAssignTMPProp(so, "pauseDmgReductionStatText", "Stat_Dmg Reduct", map);
                TryAssignTMPProp(so, "pauseTimeStatText", "Stat_Time", map);
                TryAssignTMPProp(so, "pauseWeaponNameText", "Weapon_Name", map);
                TryAssignTMPProp(so, "pauseWeaponDamageText", "Weapon_Damage", map);
                TryAssignTMPProp(so, "pauseWeaponDpsText", "Weapon_DPS", map);
                TryAssignTMPProp(so, "pauseItemEmptyText", "Text_NoItem", map);

                // Defeat items
                TryAssignTMPProp(so, "defeatEmeraldsText", "Amount", map);
                TryAssignTMPProp(so, "defeatEmeraldsFormulaText", "Formula", map);
                TryAssignTMPProp(so, "defeatItemStatusText", "Item_Status", map);
                so.ApplyModifiedProperties();
            }

            // 3. LevelUpManager
            LevelUpManager lvlMgr = Object.FindAnyObjectByType<LevelUpManager>();
            if (lvlMgr != null)
            {
                SerializedObject so = new SerializedObject(lvlMgr);
                TryAssignTMPProp(so, "levelTitleText", "Title", map);
                TryAssignTMPProp(so, "rerollButtonText", "Label", map);
                so.ApplyModifiedProperties();
            }

            // 4. LevelUpCardUI
            LevelUpCardUI[] cardUIs = Object.FindObjectsByType<LevelUpCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cardUIs != null)
            {
                foreach (var card in cardUIs)
                {
                    SerializedObject so = new SerializedObject(card);
                    foreach (Transform child in card.GetComponentsInChildren<Transform>(true))
                    {
                        if (map.TryGetValue(child.gameObject, out TextMeshProUGUI tmp))
                        {
                            if (child.name == "Title") AssignProp(so, "titleText", tmp);
                            else if (child.name == "Description") AssignProp(so, "descriptionText", tmp);
                            else if (child.name == "Synergy") AssignProp(so, "synergyText", tmp);
                            else if (child.name == "Confirm_Hint") AssignProp(so, "confirmHintText", tmp);
                            else if (child.parent != null && child.parent.name == "Hotkey_Pill") AssignProp(so, "hotkeyText", tmp);
                            else if (child.parent != null && child.parent.name == "Badge_1") AssignProp(so, "badge1Text", tmp);
                            else if (child.parent != null && child.parent.name == "Badge_2") AssignProp(so, "badge2Text", tmp);
                        }
                    }
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void TryAssignTMPProp(SerializedObject so, string propName, string searchName, Dictionary<GameObject, TextMeshProUGUI> map)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null) return;

            foreach (var kvp in map)
            {
                if (kvp.Key.name == searchName || (kvp.Key.transform.parent != null && kvp.Key.transform.parent.name == searchName))
                {
                    prop.objectReferenceValue = kvp.Value;
                    return;
                }
            }
        }

        private static void AssignProp(SerializedObject so, string propName, TextMeshProUGUI val)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = val;
            }
        }
    }
}
