using UnityEngine;

namespace BaseAssistant
{
    public class AssistantBedMarker : MonoBehaviour, Interactable, Hoverable
    {
        private GameObject _marker;
        private bool _showMarker = false;

        private void Start()
        {
            // Criar o marcador de área roubando do workbench
            GameObject workbench = Jotunn.Managers.PrefabManager.Cache.GetPrefab<GameObject>("piece_workbench");
            if (workbench != null)
            {
                CraftingStation cs = workbench.GetComponent<CraftingStation>();
                if (cs != null && cs.m_areaMarker != null)
                {
                    _marker = Instantiate(cs.m_areaMarker, transform.position, Quaternion.identity, transform);
                    _marker.SetActive(false);
                    
                    // Workbench range é 20. O Scale original do marcador reflete isso.
                    // O raio do marcador é baseado na config. CircleProjector controla isso no Valheim.
                    float radius = Plugin.WorkRadius.Value;
                    
                    CircleProjector cp = _marker.GetComponentInChildren<CircleProjector>();
                    if (cp != null)
                    {
                        cp.m_radius = radius;
                        cp.m_nrOfSegments = Mathf.Max(32, (int)(radius * 4));
                    }
                }
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;
            
            _showMarker = !_showMarker;
            if (_marker != null) _marker.SetActive(_showMarker);
            
            user.Message(MessageHud.MessageType.Center, _showMarker ? $"Área do Assistente Visível ({Plugin.WorkRadius.Value}m)" : "Área do Assistente Oculta");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return "\n[<color=yellow><b>E</b></color>] " + (_showMarker ? "Ocultar Área de Trabalho" : "Mostrar Área de Trabalho");
        }

        public string GetHoverName()
        {
            return "Cama do Assistente";
        }
    }
}
