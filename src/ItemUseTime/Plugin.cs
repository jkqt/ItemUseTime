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
    private static GUIManager guiManager;
    private static TextMeshProUGUI itemUseTimeTextMesh;
    private static ConfigEntry<float> configFontSize;
    private static ConfigEntry<float> configOutlineWidth;

    private void Awake()
    {
        Log = Logger;
        configFontSize = ((BaseUnityPlugin)this).Config.Bind<float>("ItemUseTime", "Font Size", 20f, "Customize the Font Size for item progress circle text.");
        configOutlineWidth = ((BaseUnityPlugin)this).Config.Bind<float>("ItemUseTime", "Outline Width", 0.08f, "Customize the Outline Width for item progress circle text.");
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
                    updateitemUseTimeTextMesh(__instance);
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.Message + e.StackTrace);
            }
        }
    }

    private static void updateitemUseTimeTextMesh(UI_UseItemProgress useItemProgress)
    {
        Character character = Character.observedCharacter;

        if (character != null)
        {
            if (character.photonView.IsMine)
            {
                Item currItem = character.data.currentItem; // not sure why this broke after THE MESA update, made no changes (just rebuilt)

                // character.refs.items.currentClimbingSpikeComponent might be null at some point, could move trygetcomponent into a nested if statement (but it's not throwing any error rn even if null...?)
                if ((character.refs.items.climbingSpikeCastProgress > 0f) && character.refs.items.currentClimbingSpikeComponent.gameObject.TryGetComponent<Item>(out Item climbingSpike))
                {
                    itemUseTimeTextMesh.text = ((1f - useItemProgress.fill.fillAmount) * climbingSpike.totalSecondaryUsingTime).ToString("F1");
                    itemUseTimeTextMesh.gameObject.SetActive(true);
                }
                else if ((currItem != null) && (currItem.shouldShowCastProgress))
                {
                    if (currItem.gameObject.TryGetComponent<RopeTier>(out RopeTier ropeTier))
                    {
                        itemUseTimeTextMesh.text = ((1f - useItemProgress.fill.fillAmount) * ropeTier.castTime).ToString("F1");
                    }
                    else if (currItem.isUsingSecondary)
                    {
                        itemUseTimeTextMesh.text = ((1f - useItemProgress.fill.fillAmount) * currItem.totalSecondaryUsingTime).ToString("F1");
                    }
                    else
                    {
                        itemUseTimeTextMesh.text = ((1f - useItemProgress.fill.fillAmount) * currItem.usingTimePrimary).ToString("F1");
                    }
                    itemUseTimeTextMesh.gameObject.SetActive(true);
                }
                else if ((useItemProgress.constantUseInteractableExists) && (Interaction.instance.constantInteractableProgress > 0f))
                {
                    itemUseTimeTextMesh.text = ((1f - useItemProgress.fill.fillAmount) * Interaction.instance.currentConstantInteractableTime).ToString("F1");
                    itemUseTimeTextMesh.gameObject.SetActive(true);
                }
                else
                {
                    itemUseTimeTextMesh.gameObject.SetActive(false);
                }
            }
            else
            {
                itemUseTimeTextMesh.gameObject.SetActive(false);
            }
        }
        else
        {
            itemUseTimeTextMesh.gameObject.SetActive(false);
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
        itemUseTimeTextMesh = itemUseTime.AddComponent<TextMeshProUGUI>();
        RectTransform itemUseTimeRect = itemUseTime.GetComponent<RectTransform>();

        itemUseTimeTextMesh.font = font;
        itemUseTimeTextMesh.fontSize = configFontSize.Value;
        itemUseTimeRect.offsetMin = new Vector2(0f, 0f);
        itemUseTimeRect.offsetMax = new Vector2(0f, 0f);
        itemUseTimeTextMesh.alignment = TextAlignmentOptions.Center;
        itemUseTimeTextMesh.verticalAlignment = VerticalAlignmentOptions.Capline;
        itemUseTimeTextMesh.textWrappingMode = TextWrappingModes.NoWrap;
        itemUseTimeTextMesh.text = "";
        itemUseTimeTextMesh.outlineWidth = configOutlineWidth.Value;
    }
}