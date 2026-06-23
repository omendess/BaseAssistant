# Valheim Base Assistant

Um mod para Valheim que adiciona um NPC "Dverger" autônomo focado em logística e manutenção da base.

## 🛠️ Estado Atual (Versão Finalizada da Sessão)
O Assistente já é capaz de navegar pela base, reparar estruturas quebradas em área, coletar itens caídos e armazená-los em baús, além de reabastecer fornalhas ativamente.

### Funcionalidades Implementadas:
1. **Radar e Área de Atuação:** O assistente orbita uma "Cama do Assistente" num raio padrão de 30 metros. A cama possui um toggle (Interagir) que cria uma cúpula visual para exibir a área de cobertura.
2. **Sistema de Configuração em Tempo Real (Live Reloading):** Integração com `BepInEx.Configuration`. As distâncias e lógicas podem ser ajustadas no Thunderstore (via `com.singularitydot.baseassistant.cfg`) e são atualizadas no jogo a cada 5 segundos sem necessidade de reiniciar.
3. **Reparo em Área (AoE):** Quando o NPC detecta uma peça danificada, ele anda até uma distância segura e invoca um conserto que repara todas as estruturas num raio configurável (Padrão: 10m).
4. **Logística de Cadeia (Chain Pickup):** O assistente vasculha o chão em busca de itens acumuláveis. Se pegar um item, ele procura itens similares próximos para encher as mãos antes de ir guardar, economizando viagens.
5. **Abastecimento Inteligente (Fundição Universal):** O assistente verifica todas as fornalhas e fundições da base (incluindo mods). Ele lê ativamente o que a máquina aceita e abastece com carvão/madeira e minérios disponíveis.
6. **Limites de Produção e Reserva Segura:** O assistente pausa a produção automaticamente se um certo limite de minério ou carvão já foi produzido. Além disso, ele **nunca** esvazia os baús de matérias-primas, preservando uma quantidade de reserva (`LeaveWoodAmount`, `LeaveCoalAmount`, `LeaveOreAmount`).
7. **Sistema de Desatolamento (Geodata Bypass v2):** O cálculo de distâncias agora é feito mapeando a **Borda (Bounding Box)** dos objetos.
8. **Nomes nos Baús:** O jogador pode renomear os baús e o assistente usará o nome (baseado na tradução local) para armazenar metais e ligas sem causar problemas de diferença entre maiúsculas/minúsculas.
9. **Sistema Anti-Duplicação e Reserva Global:** O assistente utiliza um HashSet estático de ZDOIDs para garantir que múltiplos assistentes não tentem pegar o mesmo item no chão simultaneamente, evitando duplicações (clonagem).
10. **Recuperação Elegante de Tarefas (Stuck Recovery):** Se o assistente ficar travado tentando buscar um item no chão, a punição foi reduzida (5s) para que ele ou outros assistentes tentem novamente mais rápido. Se o alvo for um baú, ele desiste e tenta mais tarde, limpando sempre as reservas pendentes sem vazamentos.

## ⚙️ Variáveis de Configuração (BepInEx)
O arquivo `com.singularitydot.baseassistant.cfg` permite customizar:
* `RaioDeTrabalho`: Tamanho da área de cobertura (Padrão: 30m).
* `VidaParaReparo`: Limiar para consertar (Padrão: 0.8 / 80%).
* `RaioReparoEmArea`: Tamanho do domo de reparo AoE (Padrão: 10m).
* `DistanciaFornalha`: Distância para jogar itens na fornalha (Padrão: 4.0m).
* `DistanciaBau` / `DistanciaItemChao`: Distâncias mais curtas de interação física (Padrão: 2.5m e 2.0m).
* `LeaveWoodAmount`, `LeaveCoalAmount`, `LeaveOreAmount`: Quantidade mínima de itens a serem preservados no baú.
* `MaxCoalAmount`, `MaxSmeltedMetal`: Limites globais máximos de produção de carvão e metais.

## 📝 Próximos Passos
* **Novas Integrações Industriais:** Moinhos, rodas de fiar e fornos de pão.
* **Organização Total de Estoque:** Refinar ainda mais as pontuações do baú baseado em tipos específicos (Ex: guardar resina com madeira).

