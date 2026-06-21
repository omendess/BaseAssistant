# Base Assistant - Regras de Inteligência Artificial (Design Document)

Este documento centraliza todas as regras de comportamento, prioridades e lógicas do Assistente. Ele serve como um "Manual do Cérebro da IA" para consulta e futuras melhorias.

---

## 1. O Ciclo de Tarefas (Prioridades)
A IA toma decisões a cada varredura (scan). Ela avalia o ambiente e escolhe o que fazer com base em uma lista rigorosa de prioridades. Se a Prioridade 1 não for aplicável, ele avalia a 2, e assim por diante.

*   **[PRIORIDADE 0] SOBREVIVÊNCIA E SONO (Interruptores Absolutos):**
    *   **Perigo:** Se o assistente for atacado ou houver um evento (Raid/Invasão) na base, ele abandona qualquer tarefa e corre para a Cama (ou Totem) para se esconder.
    *   **Noite:** Se anoitecer, ele encerra o expediente e vai dormir.
    *   **Desatolamento (Anti-Stuck):** Se a IA perceber que está tentando andar para o mesmo lugar há X segundos sem sair do lugar, ela abandona a tarefa, coloca o objeto na "Lista Negra" por um tempo e se teleporta para destrabar.

*   **[PRIORIDADE 1] CHAINING (Continuar o que já está na mão):**
    *   Se ele já tem "Madeira" nas mãos, a primeira coisa que ele tenta é guardar essa madeira em um baú. Se houver mais madeira no chão e ele tiver espaço na mão, ele cata a madeira do chão antes de ir ao baú.

*   **[PRIORIDADE 2] TRIAGEM (Baú Mestre / Inbox):**
    *   Ele verifica o Baú de Entrada. Se houver itens empilháveis lá, ele tira o item de lá e começa a procurar um baú de destino.

*   **[PRIORIDADE 3] MANUTENÇÃO E REPAROS:**
    *   Ele usa "visão de raio-x" telepática. Ao se aproximar de uma parede quebrada, ele ativa um "Reparo em Área" invisível que conserta todas as peças ao seu redor de uma vez só.

*   **[PRIORIDADE 4] CARVOARIA E FUNDIÇÃO:**
    *   Ele verifica os fornos. Se o forno tem espaço e o limite global de carvão/metal não foi atingido, ele vai no baú de madeira, pega a madeira e abastece o forno.

*   **[PRIORIDADE 5] LIXEIRO (Itens no chão):**
    *   Ele recolhe qualquer item empilhável largado no chão, *apenas* se ele tiver certeza absoluta de que existe um baú com espaço para guardar aquele item. (Sistema de Clarividência).

---

## 2. A Nova Lógica de Organização (Sistema de Baús Inteligentes)

Para organizar perfeitamente os itens (incluindo armas, armaduras e comidas que antes eram ignorados), a IA passa por um rigoroso funil de tomada de decisão.

### 2.1 Regra Absoluta: O Nome Escrito no Baú (ZDO Override)
Se o jogador interagir com o baú (Shift+E) e der um nome a ele, a IA **desliga a visão** sobre o que tem dentro do baú e passa a obedecer APENAS o texto.

1.  **Dicionário de Categorias (Genérico):**
    *   Se o nome for "Armas", o baú aceita APENAS `ItemType.OneHandedWeapon`, `ItemType.Bow`, etc.
    *   Se o nome for "Comida", o baú aceita APENAS `ItemType.Consumable`.
    *   *Nota: Esses nomes são customizáveis no arquivo `.cfg`.*
2.  **Match Específico (Exato):**
    *   Se o jogador escrever o nome exato do item (ex: "Madeira Nobre"), a IA guarda APENAS Madeira Nobre lá dentro.
3.  **Proibição por Erro de Digitação (Modo Restrito):**
    *   Se o jogador digitar uma palavra que não existe no dicionário da IA (ex: "Armass" ou "Baguncado"), o baú é considerado **TRANCADO**. A IA interpretará que o baú é exclusivo para um item desconhecido e nunca guardará nada nele.
4.  **A Regra do "Ignorar":**
    *   Se o nome for "Ignorar", "Privado" ou "NaoTocar" (configurável), a IA finge que o baú não existe na base.

### 2.2 Regra Clássica: Afinidade Visual (O Baú Sem Nome)
Se o jogador não escrever nada no baú, a IA usa o "Sistema de Pontuação" lendo os itens que já estão lá dentro para decidir o melhor destino:

*   **[+100 Pontos] - Stack Perfeito:** O baú já possui exatamente o mesmo item e tem espaço na pilha. (Prioridade absoluta).
*   **[+50 Pontos] - Sub-Família:** O baú possui itens da mesma classificação menor (ex: O baú tem apenas "Madeira Comum", a IA pode colocar "Madeira Core" lá dentro para juntar as madeiras).
*   **[+25 Pontos] - Família (ItemType):** O baú possui apenas Armas, então a IA assume que aquele é o baú de equipamentos.
*   **[  0 Pontos] - Baú Vazio:** Se nenhum baú no mundo combinar, a IA pega o primeiro baú vazio que achar e consagra ele para o item.
*   **[-1000 Pontos] - Contaminação Proibida:** A IA NUNCA misturará categorias primárias. Ela nunca colocará Salsichas no baú que contém Pedras, mesmo que ele tenha espaço sobrando.

### 2.3 Descontaminação de Estoque (Reorganização Ativa e Anti-Loop)
A IA possui uma rotina de auditoria interna (`ConsolidateItem`). Quando estiver ociosa (Idle), ela escaneia os baús em busca de discrepâncias organizacionais.

1.  **A Identificação Absoluta (Score 2000):** Se o texto de um baú corresponder EXATAMENTE ao nome traduzido de um item no jogo (ex: "Madeira de Qualidade"), esse baú ganha a pontuação suprema (2000) e se torna o "dono" absoluto desse item.
2.  **O Saque (Auditoria):** A IA varre todos os outros baús de menor prioridade (sem nome ou categorias genéricas). Se encontrar "Madeira de Qualidade" dentro de um baú que não tem a "Afinidade Suprema", o NPC ativamente vai até esse baú e saca o item para o próprio inventário.
3.  **O Depósito:** Em seguida, o NPC procura o destino final e, naturalmente, o sistema o guiará para o baú correto (Score 2000).
4.  **A Regra Anti-Loop:** O assistente **SÓ PODE** retirar um item de um baú se o baú de destino (conhecido na rede) tiver uma pontuação (Score) **ESTRITAMENTE MAIOR** que a do baú atual. Isso impede loops infinitos onde o NPC tira de um baú renomeado e tenta colocar em outro baú idêntico.

---

## 3. Segurança Multiplayer (Server Sync)

Para evitar que diferentes jogadores tenham regras diferentes ou "quebrem" a organização:
*   Todas as variáveis importantes de organização (Palavras-chave dos baús, ativadores das rotinas e limites de distância) ficam no arquivo `BaseAssistant.cfg`.
*   As configurações recebem a propriedade `IsAdminOnly = true` via framework Jotunn.
*   O servidor transmite seu `.cfg` para os clientes. Assim, a regra do servidor anula as configurações locais do jogador comum, garantindo que o Dicionário do Organizador seja universal para todos na mesma base.
