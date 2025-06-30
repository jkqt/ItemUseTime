using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace ItemUseTime;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    private static TextMeshProUGUI progressCircleText;
    private static GUIManager guiManager;
    private static ConfigEntry<float> configFontSize;
    private static ConfigEntry<float> configOutlineWidth;

    private void Awake()
    {
        Log = Logger;
        configFontSize = ((BaseUnityPlugin)this).Config.Bind<float>("ItemUseTime", "Font Size", 20f, "Customize the Font Size for item progress circle text.");
        configOutlineWidth = ((BaseUnityPlugin)this).Config.Bind<float>("ItemUseTime", "Outline Width", 0.1f, "Customize the Outline Width for item progress circle text.");
        Harmony.CreateAndPatchAll(typeof(ItemUseTimeProgressUpdatePatch));
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static class ItemUseTimeProgressUpdatePatch
    {
        [HarmonyPatch(typeof(UI_UseItemProgress), "Update")]
        [HarmonyPostfix]
        private static void ItemUseTimeProgressUpdate(UI_UseItemProgress __instance)
        {
            try
            {
                if (guiManager == null)
                {
                    InitItemUseTime();
                }
                else
                {
                    updateProgressCircleText(__instance);
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static void updateProgressCircleText(UI_UseItemProgress useItemProgress)
    {
        //Character character = guiManager.character;
        Character character = Character.observedCharacter;

        if (character != null)
        {
            if (character.photonView.IsMine)
            { 
                if (character.refs.items.climbingSpikeCastProgress > 0f)
                {
                    // TODO: find better access point for climbing spike conditional
                    // Climbing Spike Use Time currently hard set at 2f (can't find a good var to access due to temporary hand slot edge case not updating .currentClimbingSpikeItemSlot)
                    // might just search the scene with find for the first climbing spike within game scene...? but highly likely to break if it checks someone else's while they're using it
                    progressCircleText.text = ((1f - useItemProgress.fill.fillAmount) * 2f).ToString("F1");
                    progressCircleText.gameObject.SetActive(true);
                }
                else if ((character.data.currentItem != null) && (character.data.currentItem.shouldShowCastProgress))
                {
                    if (character.data.currentItem.isUsingSecondary)
                    {
                        progressCircleText.text = ((1f - useItemProgress.fill.fillAmount) * character.data.currentItem.totalSecondaryUsingTime).ToString("F1");
                    }
                    else
                    {
                        progressCircleText.text = ((1f - useItemProgress.fill.fillAmount) * character.data.currentItem.usingTimePrimary).ToString("F1");
                    }
                    progressCircleText.gameObject.SetActive(true);
                }
                else if ((useItemProgress.constantUseInteractableExists) && (Interaction.instance.constantInteractableProgress > 0f))
                {
                    progressCircleText.text = ((1f - useItemProgress.fill.fillAmount) * Interaction.instance.currentConstantInteractableTime).ToString("F1");
                    progressCircleText.gameObject.SetActive(true);
                }
                else
                {
                    progressCircleText.gameObject.SetActive(false);
                }
            }
            else
            {
                progressCircleText.gameObject.SetActive(false);
            }
        }
        else
        {
            progressCircleText.gameObject.SetActive(false);
        }
    }

    private static void InitItemUseTime()
    {
        GameObject guiManagerGameObj = GameObject.Find("GAME/GUIManager");
        guiManager = guiManagerGameObj.GetComponent<GUIManager>();
        TMPro.TMP_FontAsset font = guiManager.heroDayText.font;
        GameObject useItemGameObj = guiManagerGameObj.transform.Find("Canvas_HUD/UseItem").gameObject;
        GameObject itemUseTime = new GameObject("ItemUseTime");
        itemUseTime.transform.SetParent(useItemGameObj.transform);
        TextMeshProUGUI itemUseTimeText = itemUseTime.AddComponent<TextMeshProUGUI>();
        RectTransform itemUseTimeRect = itemUseTime.GetComponent<RectTransform>();
        itemUseTimeText.font = font;
        itemUseTimeText.fontSize = configFontSize.Value;
        itemUseTimeRect.offsetMin = new Vector2(0f, 0f);
        itemUseTimeRect.offsetMax = new Vector2(0f, 0f);
        itemUseTimeText.alignment = TextAlignmentOptions.Center;
        itemUseTimeText.verticalAlignment = VerticalAlignmentOptions.Capline;
        itemUseTimeText.textWrappingMode = TextWrappingModes.NoWrap;
        itemUseTimeText.text = "";
        progressCircleText = itemUseTimeText;
        itemUseTimeText.outlineWidth = configOutlineWidth.Value;
    }
}