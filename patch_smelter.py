import re

with open('AssistantAI.cs', 'r', encoding='utf-8') as f:
    content = f.read()

process_ai_old = """            else if (_currentTask == "MoveToChestToFetch") ExecuteFetchFromChest();
            else if (_currentTask == "MoveToChestToConsolidate") ExecuteConsolidateFetch();
            else if (_currentTask == "FeedKiln") ExecuteFeedKiln();"""

process_ai_new = """            else if (_currentTask == "MoveToChestToFetch") ExecuteFetchForSmelting();
            else if (_currentTask == "MoveToChestToConsolidate") ExecuteConsolidateFetch();
            else if (_currentTask == "FeedSmelter") ExecuteFeedSmelter();"""

content = content.replace(process_ai_old, process_ai_new)

find_task_old = """            // PRIORIDADE 4: Abastecer os Fornos (Carvoaria) respeitando o limite da config
            if (Plugin.EnableSmelting.Value)
            {
                int coalCount = CountItemInBase("Coal", hits);
                foreach (var hit in hits)
                {
                    Smelter smelter = hit.GetComponentInParent<Smelter>();
                    if (smelter != null && smelter.name.ToLower().Contains("kiln")) 
                    {
                        if (_blacklistedObjects.ContainsKey(smelter.gameObject) && Time.time < _blacklistedObjects[smelter.gameObject]) continue;
                        if (smelter.GetComponent<ZNetView>().GetZDO().GetInt("queued", 0) < smelter.m_maxOre)
                        {
                            if (coalCount < Plugin.MaxCoalAmount.Value)
                            {
                                string woodType = "";
                                Container woodChest = FindChestForWood(hits, out woodType);
                                if (woodChest != null)
                                {
                                    _holdingItem = woodType; 
                                    SetTask("MoveToChestToFetch", woodChest.gameObject);
                                    return;
                                }
                            }
                        }
                    }
                }
            }"""

find_task_new = """            // PRIORIDADE 4: Abastecer os Fornos (Carvoaria e Fundições)
            if (Plugin.EnableSmelting.Value)
            {
                if (CheckAndSetSmeltingTask(hits)) return;
            }"""

content = content.replace(find_task_old, find_task_new)

new_methods = """
        private bool CheckAndSetSmeltingTask(Collider[] hits)
        {
            foreach (var hit in hits)
            {
                Smelter smelter = hit.GetComponentInParent<Smelter>();
                if (smelter != null)
                {
                    if (_blacklistedObjects.ContainsKey(smelter.gameObject) && Time.time < _blacklistedObjects[smelter.gameObject]) continue;

                    ZNetView smelterView = smelter.GetComponent<ZNetView>();
                    if (smelterView == null || !smelterView.IsValid()) continue;

                    // 1. Verifica se precisa de FUEL (Combustível: Carvão/Madeira)
                    if (smelter.m_maxFuel > 0 && smelter.m_fuelItem != null)
                    {
                        float currentFuel = smelterView.GetZDO().GetFloat("fuel", 0f);
                        if (currentFuel < smelter.m_maxFuel)
                        {
                            string fuelName = smelter.m_fuelItem.m_itemData.m_shared.m_name;
                            int leaveAmount = fuelName.ToLower().Contains("coal") ? Plugin.LeaveCoalAmount.Value : Plugin.LeaveWoodAmount.Value;
                            Container fuelChest = FindChestForSmelterItem(hits, fuelName, leaveAmount);
                            
                            if (fuelChest != null)
                            {
                                _holdingItem = fuelName;
                                _targetSmelter = smelter.gameObject; // Usamos isso para lembrar qual smelter alimentar
                                SetTask("MoveToChestToFetch", fuelChest.gameObject);
                                return true;
                            }
                        }
                    }

                    // 2. Verifica se precisa de ORE (Minério/Madeira crua)
                    int queued = smelterView.GetZDO().GetInt("queued", 0);
                    if (queued < smelter.m_maxOre && smelter.m_conversion != null)
                    {
                        foreach (var conversion in smelter.m_conversion)
                        {
                            if (conversion.m_from == null || conversion.m_to == null) continue;
                            
                            string rawItemName = conversion.m_from.m_itemData.m_shared.m_name;
                            string producedItemName = conversion.m_to.m_itemData.m_shared.m_name;

                            // Limites de Produção
                            int producedCount = CountItemInBase(producedItemName, hits);
                            int maxAllowed = producedItemName.ToLower().Contains("coal") ? Plugin.MaxCoalAmount.Value : Plugin.MaxSmeltedMetal.Value;

                            if (producedCount < maxAllowed)
                            {
                                int leaveAmount = rawItemName.ToLower().Contains("wood") ? Plugin.LeaveWoodAmount.Value : Plugin.LeaveOreAmount.Value;
                                Container oreChest = FindChestForSmelterItem(hits, rawItemName, leaveAmount);
                                if (oreChest != null)
                                {
                                    _holdingItem = rawItemName;
                                    _targetSmelter = smelter.gameObject;
                                    SetTask("MoveToChestToFetch", oreChest.gameObject);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private GameObject _targetSmelter;

        private Container FindChestForSmelterItem(Collider[] hits, string targetItemName, int leaveAmount)
        {
            foreach (var hit in hits)
            {
                Container chest = hit.GetComponentInParent<Container>();
                if (chest != null && chest.GetInventory() != null)
                {
                    if (_blacklistedObjects.ContainsKey(chest.gameObject) && Time.time < _blacklistedObjects[chest.gameObject]) continue;
                    
                    int currentAmount = chest.GetInventory().CountItems(targetItemName);
                    if (currentAmount > leaveAmount)
                    {
                        return chest;
                    }
                }
            }
            return null;
        }
"""

content = content.replace("private void ExecuteGoToSleep()", new_methods + "\n        private void ExecuteGoToSleep()")

start_idx = content.find("private void ExecuteFetchFromChest()")
end_idx = content.find("private void ExecuteRepairPiece()")

if start_idx != -1 and end_idx != -1:
    old_block = content[start_idx:end_idx]
    
    new_block = """private void ExecuteFetchForSmelting()
        {
            float d = Plugin.ChestDistance.Value;
            if (IsCloseEnough(_targetObj, d, d))
            {
                Container chest = _targetObj.GetComponent<Container>();
                if (chest != null && chest.GetInventory() != null)
                {
                    GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>(_holdingItem);
                    if (prefab != null)
                    {
                        string locName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                        int inChest = chest.GetInventory().CountItems(locName);
                        int leaveAmount = locName.ToLower().Contains("wood") ? Plugin.LeaveWoodAmount.Value : 
                                          locName.ToLower().Contains("coal") ? Plugin.LeaveCoalAmount.Value : Plugin.LeaveOreAmount.Value;
                        
                        if (inChest > leaveAmount)
                        {
                            int amountToTake = Mathf.Min(10, inChest - leaveAmount); // Pega até 10 por vez
                            
                            chest.GetComponent<ZNetView>().ClaimOwnership();
                            chest.GetInventory().RemoveItem(locName, amountToTake);
                            
                            chest.SetInUse(true);
                            // StartCoroutine(CloseChest(chest)); // Removido pq precisa do StartCoroutine que tá no MonoBehaviour mas o C# Python string pode dar pau. Vamos deixar pra n ter erro de compilação. Mas wait, StartCoroutine tá na AssistantAI.
                            // Deixa como original:
                            StartCoroutine(CloseChest(chest));
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            _holdingAmount = amountToTake;
                            
                            if (_targetSmelter != null)
                            {
                                SetTask("FeedSmelter", _targetSmelter);
                                return;
                            }
                        }
                    }
                }
                _currentTask = "Idle";
                _monsterAI.SetFollowTarget(null);
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private void ExecuteFeedSmelter()
        {
            float d = Plugin.KilnDistance.Value;
            if (IsCloseEnough(_targetObj, d, d))
            {
                Smelter smelter = _targetObj.GetComponent<Smelter>();
                if (smelter != null)
                {
                    ZNetView smelterView = smelter.GetComponent<ZNetView>();
                    bool isFuel = smelter.m_fuelItem != null && smelter.m_fuelItem.m_itemData.m_shared.m_name == _holdingItem;
                    
                    if (isFuel)
                    {
                        float currentFuel = smelterView.GetZDO().GetFloat("fuel", 0f);
                        int spaceLeft = (int)(smelter.m_maxFuel - currentFuel);
                        int amountToFeed = Mathf.Min(spaceLeft, _holdingAmount);
                        
                        if (amountToFeed > 0)
                        {
                            smelterView.ClaimOwnership();
                            smelterView.GetZDO().Set("fuel", currentFuel + amountToFeed);
                            if (smelter.m_fuelAddedEffects != null) smelter.m_fuelAddedEffects.Create(transform.position, Quaternion.identity);
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            _holdingAmount -= amountToFeed;
                        }
                    }
                    else
                    {
                        int queued = smelterView.GetZDO().GetInt("queued", 0);
                        int spaceLeft = smelter.m_maxOre - queued;
                        int amountToFeed = Mathf.Min(spaceLeft, _holdingAmount);
                        
                        if (amountToFeed > 0)
                        {
                            smelterView.ClaimOwnership();
                            for(int i = 0; i < amountToFeed; i++)
                            {
                                smelterView.GetZDO().Set("item" + (queued + i), _holdingItem); 
                            }
                            smelterView.GetZDO().Set("queued", queued + amountToFeed);
                            if (smelter.m_oreAddedEffects != null) smelter.m_oreAddedEffects.Create(transform.position, Quaternion.identity);
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            _holdingAmount -= amountToFeed;
                        }
                    }
                    
                    if (_holdingAmount <= 0)
                    {
                        _holdingItem = "";
                        _currentTask = "Idle";
                        _monsterAI.SetFollowTarget(null);
                    }
                    else
                    {
                        _currentTask = "Idle";
                        _monsterAI.SetFollowTarget(null);
                    }
                }
                else
                {
                    _currentTask = "Idle";
                    _monsterAI.SetFollowTarget(null);
                }
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        """
    content = content.replace(old_block, new_block)

with open('AssistantAI.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Patch concluído com sucesso!")
