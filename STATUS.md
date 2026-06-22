# Base Assistant - STATUS REPORT

## Informações Gerais
* **Versão Atual:** v0.1.1
* **Status:** Estável / Produção
* **Última Atualização:** Implementação do sistema de reserva de itens e estabilização da recuperação de tarefas (anti-stuck).

## 🟢 O que está funcionando (Implementado)
1. **Invocação:** O Dverger nasce corretamente ao construir o **Totem do Assistente** e respeita o raio de ação configurável.
2. **Scanner e UI:** O domo do radar (E no totem) e as restrições de limite de base estão 100% funcionais.
3. **Organização de Baús (Score System):**
   * Respeita nomes exatos de baús (Ex: "Ferro").
   * Agruda itens por afinidade visual/categoria perfeitamente (+100 pontos para stack exato).
4. **Fundição:** Alimenta automaticamente Fornos e Fundições com Carvão e Minérios.
5. **Anti-Duplicação:** `HashSet<ZDOID>` impede firmemente que dois Dvergers peguem o mesmo item do chão ao mesmo tempo, resolvendo o bug de clonagem e lag.
6. **Desatolamento (Anti-Stuck):** O Dverger abandona tarefas bloqueadas após 5 segundos, colocando o baú ou item em uma *blacklist* temporária.
7. **Integração Modding:** Variáveis chave estão exportadas via BepInEx (limites de carvão, velocidade, raios).

## 🟡 Em Progresso / Backlog (Próximos Passos)
* **Animação de Sono:** Fazer com que o Dverger vá deitar fisicamente na **Cama do Assistente** quando escurecer. (Atualmente a cama é apenas um construtível).
* **Expansão de Indústria:** Adicionar suporte para o Moinho de Vento (Windmill), Roda de Fiar (Spinning Wheel) e Forno de Pedra (Oven) para automatizar a base em estágios mais avançados (ex: Pão, Linho).
* **Filtros Granulares:** Refinar ainda mais a pontuação de baús para permitir separação estrita de itens parecidos (ex: resina vs madeira).
* **Testes de Estresse (Large Scale):** Avaliar se a busca por Containers causa *drops* de FPS em bases "mega-colossais" (possível otimização via OverlapSphere futura).

## 🔴 Conhecido (Bugs Monitorados)
* *Nenhum bug crítico ativo.* O problema do lag e triplicação de machados foi contido na v0.1.1.
