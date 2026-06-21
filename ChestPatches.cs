using HarmonyLib;
using UnityEngine;

namespace BaseAssistant
{
    public class AssistantChestTextReceiver : MonoBehaviour, TextReceiver
    {
        public string GetText()
        {
            ZNetView nview = GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return "";
            return nview.GetZDO().GetString("text", "");
        }

        public void SetText(string text)
        {
            ZNetView nview = GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                nview.ClaimOwnership();
                nview.GetZDO().Set("text", text);
            }
        }
    }

    [HarmonyPatch(typeof(Container))]
    public static class ContainerPatches
    {
        private static void EnsureTextReceiver(Container container)
        {
            if (container.GetComponent<AssistantChestTextReceiver>() == null)
            {
                container.gameObject.AddComponent<AssistantChestTextReceiver>();
            }
        }

        [HarmonyPatch(nameof(Container.Interact))]
        [HarmonyPrefix]
        public static bool Interact_Prefix(Container __instance, Humanoid character, bool hold, bool alt)
        {
            EnsureTextReceiver(__instance);

            if (hold) return true;

            if (alt) // Shift + E
            {
                if (!PrivateArea.CheckAccess(__instance.transform.position, 0f, true, false))
                {
                    return false;
                }

                TextInput.instance.RequestText(__instance.GetComponent<AssistantChestTextReceiver>(), "Configuração do Assistente", 20);
                return false; // Evita abrir o baú normalmente
            }

            return true;
        }

        [HarmonyPatch(nameof(Container.GetHoverText))]
        [HarmonyPostfix]
        public static void GetHoverText_Postfix(Container __instance, ref string __result)
        {
            EnsureTextReceiver(__instance);
            
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                string customText = nview.GetZDO().GetString("text", "");
                if (!string.IsNullOrEmpty(customText))
                {
                    __result = $"[<color=yellow>{customText}</color>]\n" + __result;
                }
                
                __result += "\n[<color=yellow><b>L-Shift + E</b></color>] Renomear (Assistente)";
            }
        }
    }
}
