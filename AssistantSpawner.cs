using UnityEngine;

namespace BaseAssistant
{
    public class AssistantSpawner : MonoBehaviour
    {
        private ZNetView _nview;
        private GameObject _spawnedNPC;
        private const string ZDO_NPC_ID = "AssistantNPC_ID";

        private ZDOID _cachedNpcId = ZDOID.None;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            // Damos um pequeno atraso (1 segundo) para garantir que o ZNetView do mundo foi sincronizado
            Invoke(nameof(TrySpawnNPC), 1f); 
        }

        private void TrySpawnNPC()
        {
            if (_nview == null || !_nview.IsOwner()) return;

            ZDOID existingNpcId = _nview.GetZDO().GetZDOID(ZDO_NPC_ID);
            _cachedNpcId = existingNpcId;

            if (existingNpcId == ZDOID.None)
            {
                // Nunca spawnou, vamos criar um novo!
                GameObject prefab = Jotunn.Managers.PrefabManager.Cache.GetPrefab<GameObject>("BaseAssistantNPC");
                if (prefab != null)
                {
                    // Spawna o assistente um pouco à frente e ACIMA da cama para evitar ficar preso no chão
                    Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
                    _spawnedNPC = Instantiate(prefab, spawnPos, transform.rotation);
                    
                    ZNetView npcView = _spawnedNPC.GetComponent<ZNetView>();
                    if (npcView != null)
                    {
                        // Salva o ID (RG) do NPC na cama, para sabermos de quem ela é dona
                        _cachedNpcId = npcView.GetZDO().m_uid;
                        _nview.GetZDO().Set(ZDO_NPC_ID, _cachedNpcId);
                        
                        // Grava a posição da cama na memória do NPC
                        npcView.GetZDO().Set("HomeBedPos", transform.position);
                    }
                }
            }
            else
            {
                // O NPC já existe, vamos garantir que a memória dele tenha a posição da cama atualizada (pra quem já tinha o mod instalado)
                ZDO npcZdo = ZDOMan.instance.GetZDO(existingNpcId);
                if (npcZdo != null)
                {
                    npcZdo.Set("HomeBedPos", transform.position);
                }
            }
        }

        private void OnDestroy()
        {
            // Apenas o dono (quem está quebrando a cama no servidor) tem o direito de deletar o NPC
            if (_nview == null || !_nview.IsOwner()) return; 

            if (_cachedNpcId != ZDOID.None)
            {
                // Tenta encontrar o NPC carregado no mundo perto de nós
                GameObject npc = ZNetScene.instance.FindInstance(_cachedNpcId);
                if (npc != null)
                {
                    ZNetView npcView = npc.GetComponent<ZNetView>();
                    if (npcView != null)
                    {
                        npcView.ClaimOwnership();
                        
                        // Faz um efeito visual de explosão de poeira (puff) para não sumir do nada
                        GameObject puffPrefab = Jotunn.Managers.PrefabManager.Cache.GetPrefab<GameObject>("vfx_corpse_destruction");
                        if (puffPrefab != null)
                        {
                            Instantiate(puffPrefab, npc.transform.position, Quaternion.identity);
                        }

                        npcView.Destroy();
                    }
                }
                else
                {
                    // Se o NPC não estiver carregado na memória (muito longe), deletamos ele direto do banco de dados do mundo
                    ZDO zdo = ZDOMan.instance.GetZDO(_cachedNpcId);
                    if (zdo != null)
                    {
                        ZDOMan.instance.DestroyZDO(zdo);
                    }
                }
            }
        }
    }
}
