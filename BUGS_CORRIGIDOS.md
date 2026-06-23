# Registro de Bugs e Soluções (Bugs Corrigidos)

Este arquivo serve como memória do projeto para catalogar todos os problemas enfrentados e suas respectivas soluções definitivas. Sempre que um bug for identificado, ele será documentado aqui. Após a validação da correção, a solução será adicionada ao registro.

## 1. Itens desaparecendo ao abastecer Fornalhas (Smelters/Kilns)
**Status:** ✅ Corrigido e Validado

**Sintoma:** 
O NPC pegava a madeira/minério do baú, ia até a fornalha (Kiln ou Smelter), fazia a animação, mas a fornalha não iniciava a produção e o item simplesmente sumia do jogo (não caía no chão e nem produzia carvão/metal).

**Causa:**
Ao usar o método padrão do jogo `InvokeRPC("AddOre")` ou `InvokeRPC("AddFuel")`, a requisição passava pelos sistemas de validação de rede do Valheim e de mods terceiros (como o WackyDatabase ou anti-cheats). Esses mods frequentemente verificam se o `sender` (o jogador) possui de fato o item no inventário (Player Inventory). Como o NPC enviava o RPC sem um inventário de jogador válido atrelado ao sender, a requisição era tratada como inválida ou "trapaça" pelo servidor e abortada silenciosamente. Como o NPC já havia subtraído o item localmente, ele desaparecia.

**Solução Aplicada:**
Abandono total do uso de `InvokeRPC` para NPCs.
Em vez de pedir permissão para a fornalha processar o item, o Assistente agora **toma posse do objeto na rede (ZDO)** temporariamente (`smelterView.ClaimOwnership()`) e injeta os itens diretamente nos parâmetros de memória binária da fornalha:
- `smelterView.GetZDO().Set("item" + queued.ToString(), prefabName);`
- `smelterView.GetZDO().Set("queued", queued + amountToFeed);`

Isso ignora qualquer validação de mods de inventário e obriga a fornalha a aceitar o item nativamente. Em seguida, os efeitos visuais (`m_oreAddedEffects.Create`) são chamados manualmente para manter a imersão visual.

## 2. Fornalhas travadas (Congelamento de Produção) e Combustível Ultrapassando o Limite (21/20)
**Status:** ✅ Corrigido e Validado

**Sintoma:** 
O NPC ia até a fornalha, tocava a animação de abastecer repetidas vezes, mas a máquina ficava "congelada" e não produzia nenhum material (nem carvão, nem metal). Além disso, o letreiro indicava que a capacidade havia ultrapassado o máximo (ex: Carvão 21/20).

**Causa:**
1. **Transbordo (21/20):** A matemática de espaço restante (`spaceLeft`) no tanque de combustível usava um arredondamento para cima (`CeilToInt`). Caso o combustível atual fosse um valor quebrado no banco de dados (ex: 19.1), o código arredondava o espaço vazio para 1 e forçava a entrada de mais uma unidade, transbordando para 20.1 (lido como 21/20 pelo jogo).
2. **Corrupção da Fila ZDO:** Na injeção de minérios em lote ("Bulk Feed"), o NPC alterava a quantidade total na fila (ex: dizia para a máquina "você agora tem 5 itens processando"), mas só registrava o NOME do material no slot 0. Os slots 1, 2, 3 e 4 ficavam vazios na memória ZDO. Quando o sistema nativo do jogo ia tentar derreter o item do slot 1, ele puxava uma String vazia, o que causava um erro interno fatal (`NullReferenceException`), travando o script inteiro de todas as fornalhas da área.

**Solução Aplicada:**
1. Substituição do arredondamento de `Mathf.CeilToInt` para `Mathf.FloorToInt` na contagem de combustível, garantindo que a fornalha nunca receba 1 unidade a mais do que o seu limite máximo matemático.
2. Criação de um laço de repetição (`for loop`) na injeção ZDO dos minérios. Agora o Assistente escreve com precisão as strings `item0`, `item1`, `item2`, etc., até preencher completamente a quantidade declarada na variável `queued`.
3. Invocação dos RPCs puramente visuais (`AddFuelItem` e `AddOreItem`) logo após a escrita na memória ZDO, ativando as partículas e sons nativos de inserção sem depender do processamento em rede do item em si.
