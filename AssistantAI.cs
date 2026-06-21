using UnityEngine;
using Jotunn.Managers;
using System.Collections.Generic;

namespace BaseAssistant
{
    public class AssistantAI : MonoBehaviour
    {
        private ZNetView _nview;
        private Character _character;
        private MonsterAI _monsterAI;
        private Humanoid _humanoid;
        private bool _itemsHidden = false;
        
        private float _scanInterval = 2f;
        private float _timeSinceLastScan = 0f;
        private float _greetCooldown = 0f;
        private float _taskTimeout = 0f; // Para evitar que ele fique preso tentando andar pra uma parede

        // --- Memória do Assistente ---
        private GameObject _targetObj;
        private string _currentTask = "Idle"; // Idle, PickupGround, MoveToChestToStore, MoveToChestToFetch, FeedKiln
        private string _holdingItem = ""; // "Wood" ou "Coal"
        private int _holdingAmount = 0;
        private bool _weaponsCleared = false;
        private Dictionary<GameObject, float> _blacklistedObjects = new Dictionary<GameObject, float>();
        
        // --- Detecção de Travamento ---
        private Vector3 _lastPos;
        private float _stuckTimer = 0f;

        // --- Repouso ---
        private bool _isSleeping = false;
        private bool _wasNight = false;

        private static readonly string[] _sleepPhrases = new string[] {
            "Deu a minha hora, o peão tá cansado. Até amanhã!",
            "Chega por hoje, amanhã tem mais.",
            "Bati o ponto! Boa noite chefe.",
            "Indo deitar. Ninguém é de ferro, né?",
            "E chegou a hora de dormir, até amanhã!",
            "Acabou o expediente, patrão. Vou berçar.",
            "Vou capotar na cama agora. Boa noite!",
            "Sextou... ou não, mas eu vou dormir."
        };

        private static readonly string[] _wakePhrases = new string[] {
            "Bom dia! Bora trabalhar que a base não se constrói sozinha.",
            "O sol nasceu, hora de arregaçar as mangas!",
            "Mais um dia de labuta. Bom dia chefe!",
            "Acordei! Cadê as fornalhas pra encher?"
        };

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            _monsterAI = GetComponent<MonsterAI>();
            _humanoid = GetComponent<Humanoid>();
            
            if (_character != null)
                _character.m_faction = Character.Faction.Players;
        }

        private void Start()
        {
            // Transforma ele num pacifista total (Cega e ensurdece a inteligência artificial para monstros)
            if (_monsterAI != null)
            {
                _monsterAI.m_alertRange = 0f;
                _monsterAI.m_viewRange = 0f;
                _monsterAI.m_hearRange = 0f; // Fundamental para ele não ir investigar barulhos de monstros!
                _monsterAI.m_afraidOfFire = false;
                _monsterAI.m_avoidFire = false;
                
                // Remove limiares de fuga
                // _monsterAI.m_fleeIfLowHealth = 0f; // ignorado
                _monsterAI.m_fleeIfNotAlerted = false;
            }
            
            // Fica sempre no raio da base onde foi spawnado
            if (_nview != null && _nview.IsOwner() && _monsterAI != null)
            {
                _monsterAI.SetPatrolPoint();
                _monsterAI.m_randomMoveRange = Plugin.WorkRadius.Value;
                _monsterAI.m_randomMoveInterval = 5f;
            }
        }

        private void Update()
        {
            if (_nview == null || !_nview.IsOwner()) return;

            if (!_weaponsCleared && _humanoid != null && _humanoid.GetInventory() != null)
            {
                // Deixamos para limpar aqui no Update para garantir que os itens já foram carregados do save
                var items = new System.Collections.Generic.List<ItemDrop.ItemData>(_humanoid.GetInventory().GetAllItems());
                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || 
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || 
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                            item.m_shared.m_name.ToLower().Contains("crossbow") ||
                            item.m_shared.m_name.ToLower().Contains("arbalest"))
                        {
                            _humanoid.UnequipItem(item);
                            _humanoid.GetInventory().RemoveItem(item);
                        }
                    }
                    _weaponsCleared = true;
                }
            }

            if (_greetCooldown > 0f) _greetCooldown -= Time.deltaTime;
            
            // Oculta a besta/arma para ele ficar vestido mas sem a pose esquisita armada
            if (!_itemsHidden && _humanoid != null)
            {
                _humanoid.HideHandItems();
                _itemsHidden = true;
            }
            
            // Atualiza o nome acima da cabeça dele para mostrar o inventário
            if (_character != null)
            {
                if (_holdingAmount > 0)
                {
                    // Traduz visualmente usando o sistema nativo do Valheim
                    string displayItem = _holdingItem;
                    GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>(_holdingItem);
                    if (prefab != null)
                    {
                        string locName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                        displayItem = Localization.instance.Localize(locName);
                    }
                    _character.m_name = $"Assistente ({_holdingAmount}x {displayItem})";
                }
                else
                {
                    _character.m_name = "Assistente da Base";
                }
            }
            
            _timeSinceLastScan += Time.deltaTime;
            if (_timeSinceLastScan >= _scanInterval)
            {
                _timeSinceLastScan = 0f;
                CheckForPlayerGreeting();
                ProcessAI();
            }
        }

        private void CheckForPlayerGreeting()
        {
            if (_greetCooldown <= 0f)
            {
                Player closestPlayer = Player.GetClosestPlayer(transform.position, 10f);
                if (closestPlayer != null)
                {
                    transform.LookAt(closestPlayer.transform.position);
                    Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", $"Olá {closestPlayer.GetPlayerName()}, trabalhando duro na base!", false);
                    _greetCooldown = 60f;
                }
            }
        }

        private GameObject FindBed()
        {
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(GetBaseCenter(), 15f, pieces);
            foreach (Piece p in pieces)
            {
                if (p.name.StartsWith("BaseAssistantBed") || p.m_name.Contains("Cama do Assistente"))
                {
                    return p.gameObject;
                }
            }
            return null;
        }

        private GameObject FindTotem()
        {
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(GetBaseCenter(), 5f, pieces);
            foreach (Piece p in pieces)
            {
                if (p.name.StartsWith("AssistantTotem") || p.GetComponent<AssistantSpawner>() != null)
                {
                    return p.gameObject;
                }
            }
            return null;
        }

        private void ProcessAI()
        {
            bool isNight = EnvMan.instance != null && EnvMan.IsNight();
            
            if (isNight && !_wasNight)
            {
                string phrase = _sleepPhrases[UnityEngine.Random.Range(0, _sleepPhrases.Length)];
                Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", phrase, false);
                _wasNight = true;
                
                GameObject bed = FindBed();
                if (bed != null)
                {
                    DropHoldingItem();
                    SetTask("GoToSleep", bed);
                }
                else
                {
                    Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", "Trabalhei o dia todo e não me deram nem uma cama... Que exploração!", false);
                    GameObject totem = FindTotem();
                    if (totem != null)
                    {
                        DropHoldingItem();
                        SetTask("GoToSleep", totem);
                    }
                }
            }
            else if (!isNight && _wasNight)
            {
                string phrase = _wakePhrases[UnityEngine.Random.Range(0, _wakePhrases.Length)];
                Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", phrase, false);
                _wasNight = false;
                _isSleeping = false;
                _currentTask = "Idle";
                
                // Teleporte de Desatolamento
                GameObject bed = FindBed();
                if (bed != null)
                {
                    // Tenta jogar ele pra frente da cama
                    transform.position = bed.transform.position + bed.transform.forward * 1.5f + Vector3.up * 0.5f;
                }
                else
                {
                    GameObject totem = FindTotem();
                    if (totem != null)
                    {
                        transform.position = totem.transform.position + totem.transform.forward * 1.5f + Vector3.up * 0.5f;
                    }
                    else
                    {
                        transform.position = GetBaseCenter() + transform.forward * 1.5f + Vector3.up * 0.5f;
                    }
                }
                
                // Fumaça para disfarçar o teleporte
                GameObject puff = PrefabManager.Cache.GetPrefab<GameObject>("vfx_sledge_hit"); 
                if (puff != null) Instantiate(puff, transform.position, Quaternion.identity);
            }

            if (_isSleeping && isNight) return;

            // PRIORIDADE SUPREMA: SOBREVIVÊNCIA (PÂNICO)
            if (IsUnderAttack())
            {
                if (_currentTask != "Panic")
                {
                    _currentTask = "Panic";
                    Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", "Socorro! Monstros na base!", false);
                    _monsterAI.SetFollowTarget(null);
                }
                
                GameObject bed = FindBed();
                if (bed != null)
                {
                    _monsterAI.SetFollowTarget(bed);
                }
                else
                {
                    GameObject totem = FindTotem();
                    if (totem != null) _monsterAI.SetFollowTarget(totem);
                    else _monsterAI.SetFollowTarget(null);
                }
                return;
            }
            else if (_currentTask == "Panic")
            {
                _currentTask = "Idle";
                Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20f, 5f, "", "Ufa, parece seguro agora...", false);
            }

            if (_isSleeping) return;

            // Se é noite e ele está ocioso, força ele a tentar ir dormir de novo
            if (_wasNight && _currentTask == "Idle")
            {
                GameObject bed = FindBed();
                if (bed != null) SetTask("GoToSleep", bed);
                else 
                {
                    GameObject totem = FindTotem();
                    if (totem != null) SetTask("GoToSleep", totem);
                }
                return;
            }

            // Se o alvo atual foi destruído ou o tempo da tarefa expirou (15 segundos tentando ir pro mesmo lugar), reseta
            if (_currentTask != "Idle")
            {
                // Sistema de Desatolamento (Abandono de Tarefa + Blacklist)
                if (Vector3.Distance(transform.position, _lastPos) < 0.5f)
                {
                    _stuckTimer += _scanInterval;
                    if (_stuckTimer >= Plugin.StuckTimeout.Value && _targetObj != null)
                    {
                        Jotunn.Logger.LogInfo($"[AssistantAI] Preso indo para {_targetObj.name}. Abandonando tarefa!");
                        _blacklistedObjects[_targetObj] = Time.time + 60f; // Ignora por 60s
                        _currentTask = "Idle";
                        _monsterAI.SetFollowTarget(null);
                        _stuckTimer = 0f;
                        return;
                    }
                }
                else
                {
                    _stuckTimer = 0f;
                }
                _lastPos = transform.position;

                _taskTimeout -= _scanInterval;
                if (_targetObj == null || _taskTimeout <= 0f)
                {
                    Jotunn.Logger.LogWarning($"[AssistantAI] Tarefa {_currentTask} expirou ou alvo foi destruído. Voltando para Idle.");
                    _currentTask = "Idle";
                    _monsterAI.SetFollowTarget(null);
                    _stuckTimer = 0f;
                    return;
                }
            }

            string oldTask = _currentTask;

            if (_currentTask == "Idle") FindNewTask();
            else if (_currentTask == "PickupGround")
            {
                ExecutePickupGround();
            }
            else if (_currentTask == "TakeFromInbox")
            {
                ExecuteTakeFromInbox();
            }
            else if (_currentTask == "MoveToChestToStore")
            {
                ExecuteStoreToChest();
            }
            else if (_currentTask == "MoveToChestToFetch") ExecuteFetchForSmelting();
            else if (_currentTask == "MoveToChestToConsolidate") ExecuteConsolidateFetch();
            else if (_currentTask == "FeedSmelter") ExecuteFeedSmelter();
            else if (_currentTask == "RepairPiece") ExecuteRepairPiece();
            else if (_currentTask == "GoToSleep") ExecuteGoToSleep();

            if (oldTask != _currentTask)
            {
                Jotunn.Logger.LogInfo($"[AssistantAI] Mudou de tarefa: {oldTask} -> {_currentTask}");
            }
        }

        private Vector3 GetBaseCenter()
        {
            if (_nview != null && _nview.GetZDO() != null)
            {
                Vector3 bedPos = _nview.GetZDO().GetVec3("HomeBedPos", Vector3.zero);
                if (bedPos != Vector3.zero) return bedPos;
            }
            return transform.position; // Fallback se algo der errado
        }

        private GameObject _targetSmelter;

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
                                _targetSmelter = smelter.gameObject;
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

        private void ExecuteGoToSleep()
        {
            if (_targetObj == null)
            {
                _currentTask = "Idle";
                return;
            }

            float d = 3.0f; // Distância pra considerar que chegou na cama
            if (IsCloseEnough(_targetObj, d, d))
            {
                _isSleeping = true;
                _currentTask = "Idle";
                _monsterAI.SetFollowTarget(null);
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private void ExecuteRepairPiece()
        {
            if (_targetObj == null)
            {
                _currentTask = "Idle";
                return;
            }

            // Híbrido: Ele chega a até X metros (perto o suficiente para ver) e faz o reparo "telepático"
            float d = Plugin.RepairDistance.Value;
            if (IsCloseEnough(_targetObj, d, d))
            {
                // Agora o reparo é em ÁREA! Pega todas as peças ao redor do alvo num raio X.
                Collider[] hits = Physics.OverlapSphere(_targetObj.transform.position, Plugin.AoERepairRadius.Value);
                bool repairedAnything = false;

                foreach (var hit in hits)
                {
                    Piece piece = hit.GetComponentInParent<Piece>();
                    if (piece != null)
                    {
                        WearNTear wear = piece.GetComponent<WearNTear>();
                        if (wear != null && wear.GetHealthPercentage() < 1f)
                        {
                            wear.Repair();
                            piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);
                            repairedAnything = true;
                        }
                    }
                }

                if (repairedAnything)
                {
                    transform.LookAt(_targetObj.transform.position);
                    ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                    if (zanim != null) zanim.SetTrigger("interact");
                }
                
                _currentTask = "Idle";
                _monsterAI.SetFollowTarget(null);
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private int CountItemInBase(string prefabName, Collider[] hits)
        {
            GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>(prefabName);
            if (prefab == null) return 0;
            string localizedName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            int count = 0;
            foreach (var hit in hits)
            {
                Container container = hit.GetComponentInParent<Container>();
                if (container != null && container.GetInventory() != null && !container.name.Contains("personal"))
                {
                    count += container.GetInventory().CountItems(localizedName);
                }
            }
            return count;
        }

        private void FindNewTask()
        {
            Collider[] hits = Physics.OverlapSphere(GetBaseCenter(), Plugin.WorkRadius.Value); 
            
            // PRIORIDADE 1: Se tem algo nas mãos, tenta pegar mais do mesmo (chaining) ou vai guardar
            if (_holdingAmount > 0)
            {
                GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>(_holdingItem);
                int maxStack = 50;
                if (prefab != null) maxStack = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize;

                if (_holdingAmount < maxStack)
                {
                    foreach (var hit in hits)
                    {
                        ItemDrop item = hit.GetComponentInParent<ItemDrop>();
                        if (item != null && item.GetComponent<ZNetView>() != null && item.GetComponent<ZNetView>().IsValid())
                        {
                            if (_blacklistedObjects.ContainsKey(item.gameObject) && Time.time < _blacklistedObjects[item.gameObject]) continue;
                            string itemName = item.gameObject.name.Replace("(Clone)", "").Trim();
                            if (itemName == _holdingItem)
                            {
                                SetTask("PickupGround", item.gameObject);
                                return;
                            }
                        }
                    }
                }

                GameObject holdingPrefab = PrefabManager.Cache.GetPrefab<GameObject>(_holdingItem);
                if (holdingPrefab != null)
                {
                    ItemDrop.ItemData holdingItemData = holdingPrefab.GetComponent<ItemDrop>().m_itemData;
                    Container chest = FindChestFor(holdingItemData, hits);
                    if (chest != null)
                    {
                        SetTask("MoveToChestToStore", chest.gameObject);
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning($"[AssistantAI] Não achei baú com espaço para {_holdingItem}. Dropando no chão.");
                        DropHoldingItem(); // Isso vai dropar e a gente não pega mais isso por um tempo
                    }
                }
                return; 
            }

            // PRIORIDADE 2: Separar itens do Baú Mestre de Entrada (Inbox)
            if (Plugin.EnableSorting.Value)
            {
                Container inboxChest = FindInboxChestWithValidItems();
                if (inboxChest != null)
                {
                    _currentTask = "TakeFromInbox";
                    _targetObj = inboxChest.gameObject;
                    _monsterAI.SetFollowTarget(_targetObj);
                    return;
                }
            }

            // PRIORIDADE 3: Procurar o que consertar
            if (Plugin.EnableRepairing.Value)
            {
                foreach (var hit in hits)
                {
                    Piece piece = hit.GetComponentInParent<Piece>();
                    if (piece != null)
                    {
                        if (_blacklistedObjects.ContainsKey(piece.gameObject) && Time.time < _blacklistedObjects[piece.gameObject]) continue;
                        WearNTear wear = piece.GetComponent<WearNTear>();
                        if (wear != null && wear.GetHealthPercentage() < Plugin.RepairHealthThreshold.Value)
                        {
                            SetTask("RepairPiece", piece.gameObject);
                            return; 
                        }
                    }
                }
            }

            // PRIORIDADE 4: Abastecer os Fornos (Carvoaria e Fundições)
            if (Plugin.EnableSmelting.Value)
            {
                if (CheckAndSetSmeltingTask(hits)) return;
            }

            // PRIORIDADE 5: Limpar itens do chão (apenas se couber nos baús)
            if (Plugin.EnableLooting.Value)
            {
                foreach (var hit in hits)
                {
                    ItemDrop item = hit.GetComponentInParent<ItemDrop>();
                    if (item != null && item.GetComponent<ZNetView>() != null && item.GetComponent<ZNetView>().IsValid())
                    {
                        if (_blacklistedObjects.ContainsKey(item.gameObject) && Time.time < _blacklistedObjects[item.gameObject]) continue;
                        if (item.m_itemData != null) // Aceitamos qualquer item (Stack > 1 ou armas/ferramentas)
                        {
                            Container chest = FindChestFor(item.m_itemData, hits);
                            if (chest != null)
                            {
                                SetTask("PickupGround", item.gameObject);
                                return;
                            }
                        }
                    }
                }
            }

            // PRIORIDADE 6: Consolidar/Reorganizar Estoque
            if (Plugin.EnableSorting.Value)
            {
                if (CheckAndSetConsolidationTask(hits)) return;
            }
        }

        private Container FindChestFor(ItemDrop.ItemData targetItem, Collider[] hits)
        {
            if (targetItem == null || targetItem.m_shared == null) return null;

            Container bestChest = null;
            int bestScore = -1;

            foreach (var hit in hits)
            {
                Container container = hit.GetComponentInParent<Container>();
                if (container != null && container.GetInventory() != null && !container.name.Contains("personal"))
                {
                    if (_blacklistedObjects.ContainsKey(container.gameObject) && Time.time < _blacklistedObjects[container.gameObject]) continue;
                    if (!container.GetInventory().CanAddItem(targetItem)) continue;

                    string customName = "";
                    ZNetView zNetView = container.GetComponent<ZNetView>();
                    if (zNetView != null && zNetView.IsValid() && zNetView.GetZDO() != null)
                    {
                        customName = zNetView.GetZDO().GetString("text", "").ToLower().Trim();
                    }

                    int score = GetChestScore(container, targetItem, customName);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestChest = container;
                    }
                }
            }
            
            return bestScore >= 0 ? bestChest : null;
        }

        private int GetChestScore(Container container, ItemDrop.ItemData targetItem, string customName)
        {
            string rawName = targetItem.m_shared.m_name ?? "";
            string targetName = rawName.ToLower();
            ItemDrop.ItemData.ItemType targetType = targetItem.m_shared.m_itemType;

            if (!string.IsNullOrEmpty(customName))
            {
                if (MatchAny(customName, Plugin.TagIgnore.Value.ToLower().Split(','))) return -1000;

                string localizedName = Localization.instance.Localize(rawName).ToLower().Trim();
                if (customName == localizedName) return 2000; // Supremo

                bool matchesCustom = false;
                if (customName.Contains(targetName.Replace("$item_", "")) || targetName.Contains(customName)) matchesCustom = true;
                else
                {
                    if (MatchAny(customName, Plugin.TagWeapons.Value.ToLower().Split(',')) && IsWeapon(targetType)) matchesCustom = true;
                    if (MatchAny(customName, Plugin.TagArmor.Value.ToLower().Split(',')) && IsArmor(targetType)) matchesCustom = true;
                    if (MatchAny(customName, Plugin.TagFood.Value.ToLower().Split(',')) && targetType == ItemDrop.ItemData.ItemType.Consumable) matchesCustom = true;
                    if (MatchAny(customName, Plugin.TagWood.Value.ToLower().Split(',')) && IsWood(targetName)) matchesCustom = true;
                    if (MatchAny(customName, Plugin.TagMetal.Value.ToLower().Split(',')) && IsMetal(targetName)) matchesCustom = true;
                }

                return matchesCustom ? 1000 : -1000;
            }

            if (container.GetInventory().NrOfItems() == 0) return 0; // Baú vazio

            bool hasContamination = false;
            bool hasSameType = false;
            bool hasSameSubCategory = false;
            bool hasExactItem = false;

            foreach (var invItem in container.GetInventory().GetAllItems())
            {
                if (invItem == null || invItem.m_shared == null) continue;
                string invName = invItem.m_shared.m_name?.ToLower() ?? "";
                if (invName == targetName) hasExactItem = true;
                else if (IsWood(targetName) && IsWood(invName)) hasSameSubCategory = true;
                else if (IsMetal(targetName) && IsMetal(invName)) hasSameSubCategory = true;
                else if (invItem.m_shared.m_itemType == targetType) hasSameType = true;
                else hasContamination = true;
            }

            if (hasContamination) return -1000;
            if (hasExactItem) return 100;
            if (hasSameSubCategory) return 50;
            if (hasSameType) return 25;
            
            return -1000;
        }

        private bool CheckAndSetConsolidationTask(Collider[] hits)
        {
            foreach (var hit in hits)
            {
                Container sourceChest = hit.GetComponentInParent<Container>();
                if (sourceChest == null || sourceChest.GetInventory() == null || sourceChest.name.Contains("personal")) continue;
                if (_blacklistedObjects.ContainsKey(sourceChest.gameObject) && Time.time < _blacklistedObjects[sourceChest.gameObject]) continue;

                string sourceCustomName = "";
                ZNetView zNetView = sourceChest.GetComponent<ZNetView>();
                if (zNetView != null && zNetView.IsValid() && zNetView.GetZDO() != null)
                {
                    sourceCustomName = zNetView.GetZDO().GetString("text", "").ToLower().Trim();
                }

                foreach (ItemDrop.ItemData invItem in sourceChest.GetInventory().GetAllItems())
                {
                    if (invItem == null || invItem.m_shared == null || invItem.m_dropPrefab == null) continue;
                    
                    int currentScore = GetChestScore(sourceChest, invItem, sourceCustomName);
                    if (currentScore >= 2000) continue; // Já está no melhor lugar possível

                    Container bestChest = FindChestFor(invItem, hits);
                    if (bestChest != null && bestChest != sourceChest)
                    {
                        string bestCustomName = "";
                        ZNetView bestNetView = bestChest.GetComponent<ZNetView>();
                        if (bestNetView != null && bestNetView.IsValid() && bestNetView.GetZDO() != null)
                        {
                            bestCustomName = bestNetView.GetZDO().GetString("text", "").ToLower().Trim();
                        }

                        int bestScore = GetChestScore(bestChest, invItem, bestCustomName);
                        if (bestScore > currentScore) // STRICT ANTI-LOOP
                        {
                            _holdingItem = invItem.m_dropPrefab.name;
                            SetTask("MoveToChestToConsolidate", sourceChest.gameObject);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool MatchAny(string text, string[] tags)
        {
            foreach(string t in tags) { if (t.Trim() != "" && text.Contains(t.Trim())) return true; }
            return false;
        }

        private bool IsWeapon(ItemDrop.ItemData.ItemType t)
        {
            return t == ItemDrop.ItemData.ItemType.OneHandedWeapon || t == ItemDrop.ItemData.ItemType.TwoHandedWeapon || t == ItemDrop.ItemData.ItemType.Bow || t == ItemDrop.ItemData.ItemType.Tool;
        }

        private bool IsArmor(ItemDrop.ItemData.ItemType t)
        {
            return t == ItemDrop.ItemData.ItemType.Shield || t == ItemDrop.ItemData.ItemType.Helmet || t == ItemDrop.ItemData.ItemType.Chest || t == ItemDrop.ItemData.ItemType.Legs || t == ItemDrop.ItemData.ItemType.Shoulder;
        }

        private bool IsWood(string name)
        {
            return name.Contains("wood") || name.Contains("log") || name.Contains("bark") || name.Contains("madeira") || name.Contains("lenha");
        }

        private bool IsMetal(string name)
        {
            return name.Contains("ore") || name.Contains("scrap") || name.Contains("copper") || name.Contains("iron") || name.Contains("silver") || name.Contains("tin") || name.Contains("bronze") || name.Contains("metal") || name.Contains("flametal");
        }

        private Container FindChestForWood(Collider[] hits, out string foundWoodType)
        {
            foundWoodType = "Wood";
            GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>("Wood");
            if (prefab == null) return null;
            string localizedName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

            foreach (var hit in hits)
            {
                Container container = hit.GetComponentInParent<Container>();
                if (container != null && container.GetInventory() != null && !container.name.Contains("personal"))
                {
                    if (_blacklistedObjects.ContainsKey(container.gameObject) && Time.time < _blacklistedObjects[container.gameObject]) continue;
                    if (container.GetInventory().CountItems(localizedName) > 0)
                    {
                        return container;
                    }
                }
            }
            
            foundWoodType = "";
            return null;
        }

        private void SetTask(string taskName, GameObject target)
        {
            _currentTask = taskName;
            _targetObj = target;
            _taskTimeout = Plugin.TaskTimeout.Value;
            _monsterAI.SetFollowTarget(_targetObj);
        }

        private bool IsCloseEnough(GameObject target, float maxDistXZ = 1.5f, float maxDistY = 2.0f)
        {
            if (target == null) return false;

            // NavMesh buffer: Valheim's pathfinding often stops ~2.5m away from items near walls
            maxDistXZ += 1.5f;
            maxDistY += 1.5f;

            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                float closestDistXZ = float.MaxValue;
                float closestDistY = float.MaxValue;

                foreach (var col in colliders)
                {
                    if (col.isTrigger) continue;

                    Vector3 closestPoint = col.bounds.ClosestPoint(transform.position);
                    
                    float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(closestPoint.x, closestPoint.z));
                    float distY = Mathf.Abs(transform.position.y - closestPoint.y);

                    if (distXZ < closestDistXZ) closestDistXZ = distXZ;
                    if (distY < closestDistY) closestDistY = distY;
                }

                if (closestDistXZ != float.MaxValue)
                {
                    Jotunn.Logger.LogInfo($"[AssistantAI] Alvo: {target.name} | Dist Atual -> XZ: {closestDistXZ:F2} / Y: {closestDistY:F2} | Config -> XZ: {maxDistXZ:F2} / Y: {maxDistY:F2}");
                    // Estamos medindo da BORDA da colisão
                    return closestDistXZ < maxDistXZ && closestDistY < maxDistY;
                }
            }

            // Fallback se o objeto não tiver colisões ativas
            float fallbackDistXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.transform.position.x, target.transform.position.z));
            float fallbackDistY = Mathf.Abs(transform.position.y - target.transform.position.y);
            Jotunn.Logger.LogInfo($"[AssistantAI] Alvo (Sem Colisão): {target.name} | Dist Atual -> XZ: {fallbackDistXZ:F2} / Y: {fallbackDistY:F2} | Config -> XZ: {maxDistXZ+2f:F2} / Y: {maxDistY+0.5f:F2}");
            return fallbackDistXZ < (maxDistXZ + 2.0f) && fallbackDistY < (maxDistY + 0.5f); 
        }

        private void ExecutePickupGround()
        {
            float d = Plugin.GroundItemDistance.Value;
            if (IsCloseEnough(_targetObj, d, d))
            {
                ItemDrop item = _targetObj.GetComponent<ItemDrop>();
                if (item != null)
                {
                    // Pega o nome do Prefab limpando o "(Clone)"
                    string prefabName = item.gameObject.name.Replace("(Clone)", "").Trim();
                    
                    if (string.IsNullOrEmpty(_holdingItem) || _holdingItem == prefabName)
                    {
                        _holdingItem = prefabName;
                        _holdingAmount += item.m_itemData.m_stack;
                    }
                    else
                    {
                        // Fallback se algo bugar
                        _holdingItem = prefabName;
                        _holdingAmount = item.m_itemData.m_stack;
                    }
                    
                    // Animação do Dverger pegando
                    ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                    if (zanim != null) zanim.SetTrigger("interact");

                    // Deleta o item do chão
                    if (item.GetComponent<ZNetView>() != null) item.GetComponent<ZNetView>().Destroy();
                    else Destroy(item.gameObject);
                    
                    _currentTask = "Idle";
                    _monsterAI.SetFollowTarget(null);
                }
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private void ExecuteStoreToChest()
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
                        ItemDrop drop = prefab.GetComponent<ItemDrop>();
                        int initialAmount = _holdingAmount;
                        
                        chest.GetComponent<ZNetView>().ClaimOwnership();

                        // Adiciona de 1 em 1 para preencher apenas o espaço vazio
                        while (_holdingAmount > 0)
                        {
                            ItemDrop.ItemData clone = drop.m_itemData.Clone();
                            clone.m_stack = 1;
                            clone.m_dropPrefab = prefab; // <--- EXTREMAMENTE IMPORTANTE PARA O SALVAMENTO!
                            if (chest.GetInventory().CanAddItem(clone))
                            {
                                chest.GetInventory().AddItem(clone);
                                _holdingAmount--;
                            }
                            else
                            {
                                break; // Baú encheu
                            }
                        }
                        
                        if (initialAmount != _holdingAmount)
                        {
                            // Ele guardou pelo menos alguma coisa!
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            chest.SetInUse(true);
                            StartCoroutine(CloseChest(chest));
                        }

                        if (_holdingAmount == 0)
                        {
                            // Guardou tudo com sucesso
                            _holdingItem = "";
                            _currentTask = "Idle";
                            _monsterAI.SetFollowTarget(null);
                        }
                        else
                        {
                            // Sobrou coisa na mão porque o baú lotou!
                            Jotunn.Logger.LogInfo($"[AssistantAI] O baú encheu, sobrou {_holdingAmount} {_holdingItem} na mão.");
                            _blacklistedObjects[chest.gameObject] = Time.time + 120f; // Ignora esse baú por 2 minutos
                            _currentTask = "Idle";
                            _monsterAI.SetFollowTarget(null);
                            // No próximo tick, ele vai rodar FindNewTask() com o que sobrou na mão e procurar outro baú!
                        }
                        
                        transform.LookAt(chest.transform.position);
                    }
                }
                else
                {
                    DropHoldingItem();
                }
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private System.Collections.IEnumerator CloseChest(Container chest)
        {
            yield return new WaitForSeconds(1.5f);
            if (chest != null)
            {
                chest.SetInUse(false);
            }
        }

        private void DropHoldingItem()
        {
            if (_holdingAmount <= 0 || string.IsNullOrEmpty(_holdingItem)) return;
            
            GameObject prefab = PrefabManager.Cache.GetPrefab<GameObject>(_holdingItem);
            if (prefab != null)
            {
                // Joga o item no chão na frente dele
                GameObject dropped = Instantiate(prefab, transform.position + transform.forward * 1f + Vector3.up * 0.5f, Quaternion.identity);
                ItemDrop drop = dropped.GetComponent<ItemDrop>();
                if (drop != null)
                {
                    drop.m_itemData.m_stack = _holdingAmount;
                    drop.m_itemData.m_dropPrefab = prefab; // EXTREMAMENTE IMPORTANTE PARA SALVAMENTO DO MUNDO!
                    // Registra no Valheim que um item novo foi dropado (znetview)
                    if (dropped.GetComponent<ZNetView>() != null)
                    {
                        dropped.GetComponent<ZNetView>().GetZDO().Set("stack", _holdingAmount);
                    }
                }
            }
            _holdingAmount = 0;
            _holdingItem = "";
            _currentTask = "Idle";
            _monsterAI.SetFollowTarget(null);
        }

        private void ExecuteConsolidateFetch()
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
                        ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData;
                        string localizedName = itemData.m_shared.m_name;
                        int currentAmount = chest.GetInventory().CountItems(localizedName);
                        
                        if (currentAmount > 0)
                        {
                            int amountToTake = Mathf.Min(itemData.m_shared.m_maxStackSize, currentAmount);
                            
                            chest.GetComponent<ZNetView>().ClaimOwnership();
                            chest.GetInventory().RemoveItem(localizedName, amountToTake);
                            
                            chest.SetInUse(true);
                            StartCoroutine(CloseChest(chest));

                            _holdingAmount = amountToTake;
                            Jotunn.Logger.LogInfo($"[AssistantAI] Saquei {_holdingAmount} {_holdingItem} para organizar/descontaminar.");
                        }
                    }
                }
                
                _currentTask = "Idle";
                _monsterAI.SetFollowTarget(null);
            }
        }

        private void ExecuteFetchForSmelting()
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
                    
                    // Verifica se o item sendo segurado é FUEL
                    bool isFuel = smelter.m_fuelItem != null && smelter.m_fuelItem.m_itemData.m_shared.m_name == _holdingItem;
                    
                    if (isFuel)
                    {
                        float currentFuel = smelterView.GetZDO().GetFloat("fuel", 0f);
                        int spaceLeft = Mathf.CeilToInt(smelter.m_maxFuel - currentFuel);
                        int amountToFeed = Mathf.Min(spaceLeft, _holdingAmount);

                        if (amountToFeed > 0)
                        {
                            smelterView.ClaimOwnership();
                            smelterView.GetZDO().Set("fuel", currentFuel + amountToFeed);
                            smelter.m_fuelAddedEffects.Create(transform.position, Quaternion.identity);
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            _holdingAmount -= amountToFeed;
                            if (_holdingAmount <= 0)
                            {
                                _holdingItem = "";
                                _currentTask = "Idle";
                                _monsterAI.SetFollowTarget(null);
                            }
                            return;
                        }
                    }
                    else // Se não é fuel, então é ORE (minério / madeira para carvoaria)
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
                            smelter.m_oreAddedEffects.Create(transform.position, Quaternion.identity);
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            _holdingAmount -= amountToFeed;
                            if (_holdingAmount <= 0)
                            {
                                _holdingItem = "";
                                _currentTask = "Idle";
                                _monsterAI.SetFollowTarget(null);
                            }
                            return;
                        }
                    }

                    _currentTask = "Idle"; // Forno cheio
                    _monsterAI.SetFollowTarget(null);
                }
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        private bool IsUnderAttack()
        {
            // Pânico: Se ele tomou dano (foi atacado por um monstro)
            if (_character.GetHealth() < _character.GetMaxHealth()) return true;

            // Pânico: Se houver um evento (Raid) ativo na posição dele
            if (RandEventSystem.instance != null && RandEventSystem.instance.GetCurrentRandomEvent() != null)
            {
                var ev = RandEventSystem.instance.GetCurrentRandomEvent();
                // Apenas se ele estiver dentro do raio do evento
                if (Vector3.Distance(transform.position, ev.m_pos) <= 100f) // 100f é um raio seguro para considerar que a base está sob ataque
                {
                    return true;
                }
            }

            return false;
        }

        private Container FindInboxChestWithValidItems()
        {
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(transform.position, Plugin.WorkRadius.Value, pieces);
            
            foreach (Piece p in pieces)
            {
                // Identifica se é o baú mestre
                if (p.name.StartsWith("AssistantInboxChest") || p.m_name.Contains("Caixa de Entrada"))
                {
                    Container inbox = p.GetComponent<Container>();
                    if (inbox != null && inbox.GetInventory().GetAllItems().Count > 0)
                    {
                        foreach (ItemDrop.ItemData item in inbox.GetInventory().GetAllItems())
                        {
                            // Apenas itens estocáveis e que já tenham destino em outro baú
                            if (item.m_shared.m_maxStackSize > 1 && HasDestinationChestForItem(item.m_shared.m_name))
                            {
                                return inbox;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private bool HasDestinationChestForItem(string localizedName)
        {
            if (string.IsNullOrEmpty(localizedName)) return false;
            
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(transform.position, Plugin.WorkRadius.Value, pieces);
            foreach (Piece p in pieces)
            {
                // Ignora os próprios inboxes e baús pessoais
                if (p.name.StartsWith("AssistantInboxChest") || p.m_name.Contains("Caixa de Entrada") || p.name.Contains("personal")) continue;

                Container c = p.GetComponent<Container>();
                if (c != null && c.GetInventory() != null)
                {
                    Inventory inv = c.GetInventory();
                    if (inv.HaveItem(localizedName))
                    {
                        bool hasSpace = false;
                        foreach(var i in inv.GetAllItems())
                        {
                            if (i.m_shared.m_name == localizedName && i.m_stack < i.m_shared.m_maxStackSize) 
                            { 
                                hasSpace = true; 
                                break; 
                            }
                        }
                        if (!hasSpace && inv.GetEmptySlots() > 0) hasSpace = true;
                        
                        if (hasSpace) return true;
                    }
                }
            }
            return false;
        }

        private void ExecuteTakeFromInbox()
        {
            float d = Plugin.ChestDistance.Value;
            if (IsCloseEnough(_targetObj, d, d))
            {
                Container inbox = _targetObj.GetComponent<Container>();
                if (inbox != null && inbox.GetInventory() != null)
                {
                    foreach (ItemDrop.ItemData item in inbox.GetInventory().GetAllItems())
                    {
                        if (item.m_shared.m_maxStackSize > 1 && HasDestinationChestForItem(item.m_shared.m_name))
                        {
                            int amountToTake = Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize);
                            
                            // O _holdingItem usa o prefab name!
                            _holdingItem = item.m_dropPrefab?.name; 
                            if (string.IsNullOrEmpty(_holdingItem)) continue;

                            inbox.GetComponent<ZNetView>().ClaimOwnership();
                            inbox.GetInventory().RemoveItem(item, amountToTake);
                            
                            _holdingAmount = amountToTake;
                            
                            ZSyncAnimation zanim = GetComponent<ZSyncAnimation>();
                            if (zanim != null) zanim.SetTrigger("interact");
                            
                            inbox.SetInUse(true);
                            StartCoroutine(CloseChest(inbox));
                            
                            // Som de pegar
                            ZSFX sfx = _targetObj.GetComponentInChildren<ZSFX>();
                            if (sfx != null) sfx.Play();
                            
                            break;
                        }
                    }
                }
                _currentTask = "Idle";
                _monsterAI.SetFollowTarget(null);
                FindNewTask(); // Já emenda para procurar onde guardar
            }
            else
            {
                _monsterAI.SetFollowTarget(_targetObj);
            }
        }

        // Fim da IA
    }
}
