# Valheim Base Assistant

Um mod para Valheim que adiciona um NPC "Dverger" autônomo focado em logística e manutenção da base.

## 🛠️ Estado Atual (Versão Finalizada da Sessão)
O Assistente já é capaz de navegar pela base, reparar estruturas quebradas em área, coletar itens caídos e armazená-los em baús, além de reabastecer fornalhas ativamente.

### Funcionalidades Implementadas:
1. **Radar e Área de Atuação:** O assistente orbita uma "Cama do Assistente" num raio padrão de 30 metros. A cama possui um toggle (Interagir) que cria uma cúpula visual para exibir a área de cobertura.
2. **Sistema de Configuração em Tempo Real (Live Reloading):** Integração com `BepInEx.Configuration`. As distâncias e lógicas podem ser ajustadas no Thunderstore (via `com.omen.baseassistant.cfg`) e são atualizadas no jogo a cada 5 segundos sem necessidade de reiniciar.
3. **Reparo em Área (AoE):** Quando o NPC detecta uma peça danificada, ele anda até uma distância segura e invoca um conserto que repara todas as estruturas num raio configurável (Padrão: 10m).
4. **Logística de Cadeia (Chain Pickup):** O assistente vasculha o chão em busca de itens acumuláveis. Se pegar um item, ele procura itens similares próximos para encher as mãos antes de ir guardar, economizando viagens.
5. **Abastecimento Inteligente:** Coleta madeira de baús próximos e abastece `Smelters` (como a Charcoal Kiln).
6. **Sistema de Desatolamento (Geodata Bypass v2):** O cálculo de distâncias agora é feito mapeando a **Borda (Bounding Box)** dos objetos, ao invés do centro. Se o NPC se prender no cenário, há um timeout (5s) que teleporta o NPC com segurança para fora das bordas do objeto problemático.

## ⚙️ Variáveis de Configuração (BepInEx)
O arquivo `com.omen.baseassistant.cfg` permite customizar:
* `RaioDeTrabalho`: Tamanho da área de cobertura (Padrão: 30m).
* `VidaParaReparo`: Limiar para consertar (Padrão: 0.8 / 80%).
* `RaioReparoEmArea`: Tamanho do domo de reparo AoE (Padrão: 10m).
* `DistanciaParaConserto`: Range da 'telepatia' de reparo (Padrão: 6.0m).
* `DistanciaFornalha`: Distância para jogar itens na fornalha (Padrão: 4.0m).
* `DistanciaBau` / `DistanciaItemChao`: Distâncias mais curtas de interação física (Padrão: 2.5m e 2.0m).

## 📝 Backlog / Próximos Passos
* **Refinar Lógica de Distância e NavMesh:** O usuário relatou que a lógica de "chegada" híbrida ainda precisa de ajustes finos (NPC esbarra em problemas de pathfinding do Valheim).
* **IA de Repouso:** Implementar rotina noturna, onde o NPC volta para a cama para dormir caso não haja trabalho.
* **Filtros Personalizados:** Opção para definir quais baús recebem quais itens (Whitelist/Blacklist).

---
*Documentação gerada pelo Sistema de Encerramento (Antigravity).*
