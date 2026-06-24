# ⚙️ Valheim Base Assistant | Master Wiki & Guide

*(A versão em Português encontra-se na metade inferior desta página / The Portuguese version is on the bottom half of this page).*

---

## 🇺🇸 ENGLISH DOCUMENTATION

### 1. How to Use the Base Assistant
Getting your AI helper up and running is extremely simple.

**1.1 Build the Assistant Totem**
Equip your Hammer and look for the **Base Assistant** category. Select the **Assistant Totem** and place it anywhere in your base. This totem is the "anchor" point for your NPC.

**1.2 Meet your Assistant**
As soon as the totem is built, the Dverger Assistant will spawn and immediately start scanning the area for tasks. If he doesn't have anything to do, he will wander around near the totem.

**1.3 Visualizing the Work Area**
Interact with the totem (Press `E`) to toggle the **Radar Dome**. A glowing blue sphere will appear, showing you the exact radius (default 30m) where the assistant operates. Anything outside this dome is ignored.

**1.4 Sleep and Danger**
The Assistant has basic survival instincts:
* **Nighttime:** When night falls, the assistant goes to sleep (you can build an **Assistant Bed** for him!).
* **Raids/Danger:** If the base is attacked, the assistant abandons all tasks and hides near the totem until the threat is cleared.

**1.5 Managing Tasks**
You don't need to assign tasks manually. Simply drop items on the floor, leave damaged walls, or place empty smelters within the dome, and the AI will prioritize them automatically!

### 2. Chest Organization Rules
The Base Assistant has a highly advanced logic system for storing items. It prevents messy chests and respects your base layout.

**2.1 Visual Affinity (Auto-Sorting)**
If you don't name your chests, the AI will look inside every chest in the radius and score them:
* **Perfect Match (+100):** If a chest already has Wood and has free stack space, the assistant will ALWAYS put more Wood there.
* **Category Match (+50):** If a chest is full of tools/weapons, the AI will place other weapons there, even if they aren't exactly the same.
* **Strict Prevention:** The AI will never mix food with ores, or weapons with mob drops. If no suitable chest is found, it will look for a completely empty chest to start a new category.

**2.2 Custom Chest Names (Overrides)**
You can force the assistant to obey strict rules by renaming your chests (using Valheim's standard `Shift+E` or a sign/nameplate mod):
* **Specific Items:** Name a chest exactly `Iron` or `Ferro` (based on your game language), and the AI will ONLY put that specific item inside.
* **Categories:** Name a chest `Weapons` or `Food`, and the AI will lock that chest for those specific item types.
* **Blacklist:** Name a chest `Ignore` or `Private`, and the AI will pretend that chest doesn't exist.

**2.3 Anti-Duplication System**
Running multiple assistants? No problem! The mod features a global reservation system. When an assistant decides to pick up an item, that item is instantly locked. Other NPCs will ignore it, preventing cloning and item loss bugs.

### 3. Configuration (Live Reload)
You can tweak the Assistant's brain in real-time using the `com.singularitydot.baseassistant.cfg` file via Thunderstore Mod Manager or BepInEx Configuration Manager. Changes apply immediately in-game without restarting!

**General Settings**
* `WorkRadius` (30.0): The max distance the assistant will travel from the totem.
* `NpcWalkSpeed` (2.5) / `NpcRunSpeed` (5.0): How fast the Dverger moves.

**Production Limits & Safety**
* `MaxCoalAmount` (200): If you have this much Coal in your chests, the assistant stops feeding the Wood Kiln.
* `Leave[Metal]Ore` (10): The assistant will smelt infinitely but will always leave this amount of raw ore (Copper, Iron, Tin, etc.) safe in your chests.
* `LeaveWoodAmount` (10): The assistant will never take the last 10 wood from your chests to fuel kilns.
* `LeaveCoalAmount` (10): Keeps an emergency reserve of coal safe from being fed into smelters.

**Repair & Interaction**
* `RepairHealthThreshold` (0.8): Starts repairing when a structure drops below 80% health.
* `AoERepairRadius` (10.0): When repairing a wall, the assistant magically repairs everything else within 10 meters of it.
* `StuckTimeout` (5.0): If the NPC is blocked by players or other NPCs for 5 seconds, it intelligently drops the task and tries something else to avoid lagging the game.

---

## 🇧🇷 DOCUMENTAÇÃO EM PORTUGUÊS

### 1. Como usar o Base Assistant
Colocar seu ajudante de Inteligência Artificial para funcionar é extremamente simples.

**1.1 Construa o Totem do Assistente**
Equipe seu Martelo e procure pela categoria **Base Assistant**. Selecione o **Totem do Assistente** e coloque-o em qualquer lugar da sua base. Este totem é o ponto "âncora" do seu NPC.

**1.2 Conheça seu Assistente**
Assim que o totem for construído, o Assistente Dverger nascerá e começará a escanear a área imediatamente atrás de tarefas. Se ele não tiver nada para fazer, ficará patrulhando perto do totem.

**1.3 Visualizando a Área de Trabalho**
Interaja com o totem (Aperte `E`) para ativar o **Domo de Radar**. Uma esfera azul brilhante aparecerá, mostrando o raio exato (padrão 30m) onde o assistente opera. Tudo que estiver fora desse domo será ignorado.

**1.4 Sono e Perigo**
O Assistente possui instintos básicos de sobrevivência:
* **Noite:** Quando anoitece, o assistente encerra o expediente e vai dormir (você pode construir uma **Cama do Assistente** para ele).
* **Invasões/Perigo:** Se a base for atacada, o assistente abandona todas as tarefas e se esconde perto do totem até que a ameaça seja eliminada.

**1.5 Gerenciando Tarefas**
Você não precisa dar ordens manualmente. Simplesmente jogue itens no chão, deixe paredes danificadas ou construa fornalhas vazias dentro do domo, e a IA fará o resto automaticamente!

### 2. Regras de Organização de Baús
O Base Assistant possui uma lógica altamente avançada para guardar itens, evitando baús bagunçados e respeitando o design da sua base.

**2.1 Afinidade Visual (Organização Automática)**
Se você não der nome aos seus baús, a IA vai olhar dentro de cada um deles e dar uma pontuação:
* **Combinação Perfeita (+100):** Se um baú já tem Madeira e espaço sobrando, o assistente SEMPRE vai colocar mais madeira lá.
* **Combinação de Categoria (+50):** Se um baú está cheio de armas/ferramentas, a IA vai colocar outras armas lá, mesmo que não sejam idênticas.
* **Prevenção Estrita:** A IA nunca vai misturar comida com minérios, ou armas com restos de monstros. Se ela não achar um baú adequado, vai procurar um baú completamente vazio para criar uma nova categoria.

**2.2 Nomes Personalizados (Substituição)**
Você pode forçar o assistente a obedecer regras absolutas renomeando seus baús (usando o padrão `Shift+E` do Valheim):
* **Item Específico:** Nomeie um baú exatamente como `Ferro` ou `Iron`, e a IA vai colocar APENAS aquele item lá dentro.
* **Categorias Genéricas:** Nomeie um baú de `Armas` ou `Comida`, e a IA trancará o baú para aceitar apenas esse tipo de item.
* **Lista Negra:** Nomeie um baú de `Ignorar` ou `Privado`, e a IA vai fingir que ele não existe (ótimo para seus itens pessoais).

**2.3 Sistema Anti-Duplicação**
Usando vários assistentes ao mesmo tempo? Sem problemas! O mod conta com um sistema de reserva global. Quando um assistente decide pegar um item no chão, esse item é travado na mesma hora. Outros NPCs vão ignorá-lo, evitando duplicações (clones) ou perda de itens.

### 3. Configurações (Live Reload)
Você pode ajustar o cérebro do Assistente em tempo real usando o arquivo `com.singularitydot.baseassistant.cfg` no Thunderstore Mod Manager. As mudanças são aplicadas na mesma hora no jogo, sem precisar reiniciar!

**Configurações Gerais**
* `WorkRadius` (30.0): A distância máxima que o assistente patrulha a partir do totem.
* `NpcWalkSpeed` (2.5) / `NpcRunSpeed` (5.0): O quão rápido o Dverger se move.

**Limites de Produção e Segurança**
* `MaxCoalAmount` (200): Se você tiver essa quantidade de Carvão nos baús, o assistente para de fabricar mais.
* `Leave[Minério]Cru` (10): O assistente derreterá metais infinitamente, mas sempre deixará uma reserva segura de minério cru (Cobre, Ferro, etc.) intocada nos baús.
* `LeaveWoodAmount` (10): O assistente nunca vai usar as últimas 10 madeiras do seu baú. (Reserva segura de emergência).
* `LeaveCoalAmount` (10): Mantém uma reserva segura de carvão para que ele não queime tudo derretendo metal.

**Reparos e Interação**
* `RepairHealthThreshold` (0.8): Começa a consertar uma estrutura quando a vida dela cai abaixo de 80%.
* `AoERepairRadius` (10.0): Ao consertar uma parede, o assistente magicamente conserta tudo que estiver a até 10 metros em volta dela de uma só vez.
* `StuckTimeout` (5.0): Se o NPC ficar travado batendo em jogadores ou em outros NPCs por 5 segundos, ele desiste da tarefa inteligentemente e vai fazer outra coisa para não lagar o jogo.
