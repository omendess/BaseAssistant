# Base Assistant

**Base Assistant** is a powerful, autonomous AI NPC that helps you manage your Valheim base. Once set up, the assistant will automatically repair structures, refuel your kilns/smelters, collect dropped items, and strictly organize your chests based on dynamic affinity scoring and customizable naming.

## Features

- **Autonomous Base Maintenance:**
  - **Repairs:** Uses "telepathic" AoE repairing to fix structures around them.
  - **Smelting & Fueling:** Automatically feeds kilns with wood (up to a configurable limit).
  - **Loot Collection:** Picks up dropped items within the base radius.

- **Intelligent Inventory Sorting:**
  - **Supreme Affinity Sorting:** Rename a chest in-game (using `Shift + E`) to the exact item name (e.g., "Fine wood"). The AI will prioritize this chest above all others for that specific item.
  - **Category Sorting:** Name a chest "Weapons", "Armor", "Wood", "Metal", or "Food" to create dedicated storage.
  - **Contamination Protection:** The AI will *never* mix different primary categories (e.g., it will never put food in a chest full of stone).
  - **Active Decontamination (Anti-Loop):** When idle, the AI will audit your generic chests, extract misplaced items, and move them to their Supreme Affinity chests.

## How to Use

1. **Summon the Assistant:** Build an **Assistant Totem** or **Assistant Bed** in your base (Available in the Hammer menu). The NPC will spawn near it.
2. **Rename Chests (Optional but Recommended):** Look at any chest and press `Shift + E` to rename it. 
   - Write the exact translated name of the item (e.g., `Wood` or `Madeira`) to lock that chest to that item.
   - Or write a category (e.g., `Weapons` or `Armas`).
3. **Blacklist Chests:** Name a chest `Ignore` or `Privado` (configurable) to make the AI completely ignore it.

## Warnings & Configuration ⚠️

- **Visual Anomalies:** The assistant interacts with chests up to a configurable distance (default 3 meters). If you increase the `ChestDistance` or `WorkRadius` too much in the config, the NPC might interact with objects from across the room, which can look strange or "macabre" (ghostly floating items).
- **Misspelled Labels:** If you rename a chest but misspell the word (e.g., "Weapns"), the AI will treat the chest as **Restricted** and will never put anything inside it to protect your organization.
- **Server Sync:** This mod features Server Sync. If installed on a dedicated server, the server's configurations (like Custom Tags and ranges) will override the clients' configurations.

---

### Pt-BR: Avisos e Configuração ⚠️

- **Anomalias Visuais:** O assistente pode interagir com baús a uma distância configurável (padrão 3 metros). **ATENÇÃO:** Alterar essas distâncias para valores muito altos no arquivo de configuração (`Edit Config`) pode deixar o visual do jogo estranho e, às vezes, macabro (com itens flutuando de longe). Modifique com cuidado!
- **Nomes Errados:** Se você renomear um baú com erro de digitação (ex: "Armass"), a IA considerará o baú **Restrito/Trancado** e não guardará nada nele, para evitar bagunçar seus itens.

## Installation

### With Thunderstore Mod Manager (Recommended)
Simply click **Install with Mod Manager**. All dependencies (BepInEx and Jotunn) will be installed automatically.

### Manual Installation
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).
2. Download this mod and extract the `BaseAssistant.dll` file.
3. Place `BaseAssistant.dll` inside your `Valheim/BepInEx/plugins/` folder.

## Author

Created by O-Men.
