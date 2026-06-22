using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using HarmonyLib;

namespace BaseAssistant
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)] // Define Jotunn como dependência obrigatória
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)] // Importante para multiplayer
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.omen.baseassistant";
        public const string PluginName = "Base Assistant";
        public const string PluginVersion = "0.1.1";

        public static ConfigEntry<float> WorkRadius;
        public static ConfigEntry<float> RepairDistance;
        public static ConfigEntry<float> ChestDistance;
        public static ConfigEntry<float> KilnDistance;
        public static ConfigEntry<float> GroundItemDistance;
        public static ConfigEntry<float> StuckTimeout;
        public static ConfigEntry<float> TaskTimeout;
        public static ConfigEntry<float> NpcWalkSpeed;
        public static ConfigEntry<float> NpcRunSpeed;
        public static ConfigEntry<float> RepairHealthThreshold;
        public static ConfigEntry<float> AoERepairRadius;
        public static ConfigEntry<int> MaxCoalAmount;
        public static ConfigEntry<int> MaxSmeltedMetal;
        public static ConfigEntry<int> LeaveWoodAmount;
        public static ConfigEntry<int> LeaveCoalAmount;
        public static ConfigEntry<int> LeaveOreAmount;

        public static ConfigEntry<bool> EnableRepairing;
        public static ConfigEntry<bool> EnableSmelting;
        public static ConfigEntry<bool> EnableLooting;
        public static ConfigEntry<bool> EnableSorting;

        public static ConfigEntry<string> TagWeapons;
        public static ConfigEntry<string> TagArmor;
        public static ConfigEntry<string> TagFood;
        public static ConfigEntry<string> TagWood;
        public static ConfigEntry<string> TagMetal;
        public static ConfigEntry<string> TagIgnore;

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private void Awake()
        {
            harmony.PatchAll();
            
            EnableRepairing = Config.Bind("1. Geral", "AtivarReparos", true, "Permite que o assistente repare estruturas danificadas.");
            EnableSmelting = Config.Bind("1. Geral", "AtivarFornalhas", true, "Permite que o assistente abasteça fornalhas e fundições.");
            EnableLooting = Config.Bind("1. Geral", "AtivarColetaDeChao", true, "Permite que o assistente pegue itens soltos no chão.");
            EnableSorting = Config.Bind("1. Geral", "AtivarOrganizacao", true, "Permite que o assistente organize itens do baú mestre para os outros baús.");

            WorkRadius = Config.Bind("1. Geral", "RaioDeTrabalho", 30f, "Raio de ação do assistente a partir do Totem.");
            RepairHealthThreshold = Config.Bind("1. Geral", "VidaParaReparo", 0.8f, "Porcentagem de vida (0.0 a 1.0) para que o assistente decida consertar uma estrutura.");
            AoERepairRadius = Config.Bind("1. Geral", "RaioReparoEmArea", 10.0f, "Raio (em metros) em volta da peça que ele vai consertar tudo de uma vez com o poder da telepatia.");
            NpcWalkSpeed = Config.Bind("1. Geral", "VelocidadeAndar", 3.0f, "Velocidade do NPC quando está andando.");
            NpcRunSpeed = Config.Bind("1. Geral", "VelocidadeCorrer", 6.0f, "Velocidade do NPC quando está correndo para concluir uma tarefa.");
            
            TagWeapons = Config.Bind("5. Regras de Baus", "TagArmas", "armas, weapons, arsenal", "Palavras-chave (separadas por vírgula) que a IA reconhecerá no nome do baú para guardar Armas e Ferramentas.");
            TagArmor = Config.Bind("5. Regras de Baus", "TagArmadura", "armaduras, armor, equipamentos", "Palavras-chave que a IA reconhecerá para guardar Armaduras e Escudos.");
            TagFood = Config.Bind("5. Regras de Baus", "TagComida", "comida, food, rango, consumiveis", "Palavras-chave que a IA reconhecerá para guardar Comidas e Poções.");
            TagWood = Config.Bind("5. Regras de Baus", "TagMadeira", "wood, lenha", "Palavras-chave que a IA reconhecerá para guardar Madeiras em geral.");
            TagMetal = Config.Bind("5. Regras de Baus", "TagMetal", "metal, minério, ore", "Palavras-chave que a IA reconhecerá para guardar Minérios e Metais fundidos.");
            TagIgnore = Config.Bind("5. Regras de Baus", "TagIgnorar", "ignorar, privado, nao tocar, ignore", "Palavras-chave que a IA reconhecerá para IGNORAR completamente o baú.");
            
            string distWarning = "\n[AVISO] Alterar as distâncias de interação pode deixar o visual do jogo estranho e algumas vezes macabro (telecinese e itens flutuando).\n[Warning] Changing interaction distances may cause weird or macabre visual behaviors (telekinesis and floating items).";

            RepairDistance = Config.Bind("2. Distancias", "DistanciaParaConserto", 6.0f, "Distância máxima para ele consertar paredes, pisos e fornalhas." + distWarning);
            KilnDistance = Config.Bind("2. Distancias", "DistanciaFornalha", 4.0f, "Distância máxima para abastecer fornalhas." + distWarning);
            ChestDistance = Config.Bind("2. Distancias", "DistanciaBau", 2.5f, "Distância máxima para guardar ou pegar itens no baú." + distWarning);
            GroundItemDistance = Config.Bind("2. Distancias", "DistanciaItemChao", 2.0f, "Distância máxima para pegar itens que estão no chão." + distWarning);
            
            StuckTimeout = Config.Bind("3. Desatolamento", "TempoAteTeleportar", 5.0f, "Tempo (em segundos) que ele precisa ficar travado para ativar o teleporte.");
            TaskTimeout = Config.Bind("3. Desatolamento", "TempoMaximoTarefa", 60.0f, "Tempo máximo (em segundos) que ele fica tentando concluir uma tarefa antes de desistir e resetar.");

            MaxCoalAmount = Config.Bind("4. Producao", "MaximoCarvao", 200, "Quantidade máxima de carvão nos baús antes do Assistente parar de abastecer a Fornalha de Carvão.");
            MaxSmeltedMetal = Config.Bind("4. Producao", "MaximoMetal", 100, "Quantidade máxima de um metal nos baús antes do Assistente parar de colocar minério na Fundição.");
            LeaveWoodAmount = Config.Bind("4. Producao", "ReservaMadeira", 10, "Quantidade mínima de segurança de madeira que ele nunca vai retirar do baú.");
            LeaveCoalAmount = Config.Bind("4. Producao", "ReservaCarvao", 10, "Quantidade mínima de segurança de carvão que ele nunca vai retirar do baú para fornalhas.");
            LeaveOreAmount = Config.Bind("4. Producao", "ReservaMinerio", 0, "Quantidade mínima de segurança de minério cru que ele deixará intocado no baú.");

            Logger.LogInfo($"O mod {PluginName} (v{PluginVersion}) foi carregado com sucesso!");
            // Registro de Peças DEVE ser feito após os prefabs vanilla carregarem
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPieces;

            // O Jotunn nos avisa quando todos os prefabs originais do jogo (Vanilla) estiverem prontos
            PrefabManager.OnVanillaPrefabsAvailable += ModifyClonedPrefabs;
            PrefabManager.OnVanillaPrefabsAvailable += CreateAssistantPrefab;
        }

        private void RegisterPieces()
        {
            PieceManager.Instance.AddPieceCategory("Hammer", "Base Assistant");

            // 1. Caixa de Entrada
            Jotunn.Configs.PieceConfig inboxConfig = new Jotunn.Configs.PieceConfig
            {
                Name = "Caixa de Entrada (Assistente)",
                Description = "Baú Mestre: Coloque seus itens aqui para o Assistente organizar automaticamente nos outros baús da base.",
                PieceTable = "Hammer",
                Category = "Base Assistant",
                Requirements = new Jotunn.Configs.RequirementConfig[]
                {
                    new Jotunn.Configs.RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
                    new Jotunn.Configs.RequirementConfig { Item = "Resin", Amount = 2, Recover = true }
                }
            };
            PieceManager.Instance.AddPiece(new CustomPiece("AssistantInboxChest", "piece_chest_wood", inboxConfig));

            // 2. Totem do Assistente
            Jotunn.Configs.PieceConfig totemConfig = new Jotunn.Configs.PieceConfig
            {
                Name = "Totem do Assistente",
                Description = "Invoque o Assistente Dverger! O totem define o centro da área de trabalho do seu assistente.",
                PieceTable = "Hammer",
                Category = "Base Assistant",
                Requirements = new Jotunn.Configs.RequirementConfig[]
                {
                    new Jotunn.Configs.RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
                    new Jotunn.Configs.RequirementConfig { Item = "GreydwarfEye", Amount = 1, Recover = true }
                }
            };
            PieceManager.Instance.AddPiece(new CustomPiece("AssistantTotem", "guard_stone", totemConfig));

            // 3. Cama do Assistente
            Jotunn.Configs.PieceConfig bedConfig = new Jotunn.Configs.PieceConfig
            {
                Name = "Cama do Assistente",
                Description = "Construa esta cama para que o seu Assistente tenha onde dormir à noite.",
                PieceTable = "Hammer",
                Category = "Base Assistant",
                Requirements = new Jotunn.Configs.RequirementConfig[]
                {
                    new Jotunn.Configs.RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
                    new Jotunn.Configs.RequirementConfig { Item = "DeerHide", Amount = 4, Recover = true },
                    new Jotunn.Configs.RequirementConfig { Item = "LeatherScraps", Amount = 4, Recover = true },
                    new Jotunn.Configs.RequirementConfig { Item = "BronzeNails", Amount = 5, Recover = true }
                }
            };
            // Tenta usar a cama normal para não quebrar o mod
            PieceManager.Instance.AddPiece(new CustomPiece("BaseAssistantBed", "bed", bedConfig));
        }

        private void ModifyClonedPrefabs()
        {
            // Modifica o Totem
            GameObject totemPiece = PrefabManager.Cache.GetPrefab<GameObject>("AssistantTotem");
            if (totemPiece != null)
            {
                PrivateArea pa = totemPiece.GetComponent<PrivateArea>();
                if (pa != null) UnityEngine.Object.DestroyImmediate(pa, true);

                // Destrói o círculo nativo do guard_stone para evitar o "bug das duas linhas"
                CircleProjector[] projectors = totemPiece.GetComponentsInChildren<CircleProjector>(true);
                foreach (var p in projectors) UnityEngine.Object.DestroyImmediate(p.gameObject, true);

                totemPiece.AddComponent<AssistantBedMarker>();
                totemPiece.AddComponent<AssistantSpawner>();
            }

            // Modifica a Cama
            GameObject bedPiece = PrefabManager.Cache.GetPrefab<GameObject>("BaseAssistantBed");
            if (bedPiece != null)
            {
                Bed originalBed = bedPiece.GetComponent<Bed>();
                if (originalBed != null) UnityEngine.Object.DestroyImmediate(originalBed, true);
            }

            PrefabManager.OnVanillaPrefabsAvailable -= ModifyClonedPrefabs;
        }

        private float _configReloadTimer = 0f;

        private void Update()
        {
            _configReloadTimer += Time.deltaTime;
            if (_configReloadTimer >= 5f)
            {
                _configReloadTimer = 0f;
                Config.Reload(); // Atualiza os valores do Thunderstore em tempo real!
            }
        }

        private void CreateAssistantPrefab()
        {
            // Pega o prefab do Dverger (anão) porque ele já tem AI, Animações de andar, e Corpo físico
            GameObject dvergerPrefab = PrefabManager.Cache.GetPrefab<GameObject>("Dverger");
            if (dvergerPrefab == null)
            {
                Logger.LogError("Não foi possível encontrar o modelo do Dverger!");
                return;
            }

            // Clona o Dverger com um novo nome: BaseAssistantNPC
            GameObject assistantPrefab = PrefabManager.Instance.CreateClonedPrefab("BaseAssistantNPC", dvergerPrefab);
            
            // Muda o nome em cima da cabeça
            var character = assistantPrefab.GetComponent<Character>();
            if (character != null)
            {
                character.m_name = "Assistente da Base";
            }
            
            // Remove drops (para não dropar os itens originais dele ao morrer)
            var charDrop = assistantPrefab.GetComponent<CharacterDrop>();
            if (charDrop != null)
            {
                UnityEngine.Object.Destroy(charDrop);
            }

            // Vamos limpar os sons/textos nativos do Dverger (o "Ham." e outros resmungos)
            var monsterAI = assistantPrefab.GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.m_idleSound = new EffectList();
            }

            // Vamos adicionar o NOSSO script de Inteligência Artificial aqui:
            assistantPrefab.AddComponent<AssistantAI>();

            // Avisa o Jotunn para adicionar nosso novo NPC no registro do jogo
            CustomCreature customCreature = new CustomCreature(assistantPrefab, fixReference: true);
            CreatureManager.Instance.AddCreature(customCreature);
            
            Logger.LogInfo("Assistente Base gerada com sucesso!");
            
            // Desregistramos o evento para não rodar duas vezes
            PrefabManager.OnVanillaPrefabsAvailable -= CreateAssistantPrefab;
        }
    }
}
