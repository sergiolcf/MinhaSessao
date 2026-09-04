---
name: iniciar-card
description: Automatiza o início de um card de desenvolvimento no projeto MinhaSessao — sincroniza a branch Develop com o remoto e cria a partir dela uma nova branch CARD-XX (checkout -b), seguindo o mesmo padrão de nomenclatura usado pelo skill finalizar-card. Use este skill sempre que o usuário disser algo como "Iniciar CARD-XX", "iniciar o card 15", "começar o CARD-XX", "cria a branch do CARD-XX", "vamos começar o card XX" ou pedir para dar início/abrir/criar a branch de um novo card — mesmo que não use exatamente essas palavras. Se a branch atual não for a Develop, este skill NÃO decide sozinho o que fazer: ele pergunta ao usuário qual ação tomar antes de prosseguir.
---

# Iniciar Card (MinhaSessao)

Workflow de git para começar um novo card de desenvolvimento neste projeto: cria uma branch
`CARD-XX` a partir de uma `Develop` atualizada, espelhando o mesmo padrão de nomenclatura que o
skill [finalizar-card](../finalizar-card/SKILL.md) usa para fechar o card no final (`feat: ... (CARD-XX)`,
depois `merge: incorpora CARD-XX (...) em Develop`).

O usuário já autorizou este fluxo ao pedir a criação deste skill — não é necessário perguntar "posso
criar a branch?" a cada execução. A única decisão que fica explicitamente com o usuário é o que
fazer quando a branch atual não é a `Develop` (ver Passo 1) — nesse caso, sempre pergunte antes de
agir, nunca escolha por conta própria.

## Passo 0 — Descobrir o número do card

O número do card vem do que o usuário digitou (ex.: "Iniciar CARD-15" → `CARD-15`). Se o usuário
disser só "Iniciar um card novo" sem número, ou disser um número que já existe (ver Passo 3),
pergunte a ele qual número usar em vez de adivinhar — é uma decisão dele, não algo que dá pra
inferir do código ou do histórico.

## Passo 1 — Confirmar que está na Develop

Rode `git branch --show-current`.

- **Se for exatamente `Develop`**: siga para o Passo 2.
- **Se não for** (`CARD-YY` de outro card ainda aberto, `main`, `Homolog`, qualquer outra coisa):
  **pare e pergunte ao usuário qual ação tomar antes de continuar** (é exatamente o comportamento
  que o usuário pediu ao criar este skill). Rode `git status` primeiro para saber se há mudanças
  pendentes na branch atual — isso muda as opções que fazem sentido oferecer. Dependendo do que
  encontrar, ofereça algo como:
  - Se a branch atual é outro `CARD-YY` com trabalho pendente: perguntar se o usuário quer
    finalizar aquele card primeiro (skill `finalizar-card`), guardar o trabalho com `git stash` e
    trocar para a Develop, ou commitar antes de trocar.
  - Se a branch atual está limpa (sem mudanças pendentes): perguntar se é só para trocar para a
    Develop (`git checkout Develop`) e prosseguir.
  - Não presuma: apresente as opções relevantes para o que `git status` mostrou e espere a resposta
    do usuário antes de rodar qualquer comando que troque de branch ou descarte trabalho.

Só siga para o Passo 2 automaticamente quando a branch atual já for a `Develop`.

## Passo 2 — Sincronizar a Develop com o remoto

```
git pull
```

Isso garante que a nova branch do card nasce a partir do código mais atual, evitando que o card
comece já desatualizado em relação ao que outras pessoas publicaram. Se o `pull` trouxer conflitos
(pouco comum numa `Develop` sem mudanças locais, mas pode acontecer se alguém commitou direto nela),
pare e avise o usuário em vez de resolver às cegas.

## Passo 3 — Checar se a branch já existe

```
git branch -a | grep -oE "CARD-[0-9]+" | sort -u
```

Se `CARD-XX` já aparecer nessa lista (local ou remota, ex.: `remotes/origin/CARD-XX`), não crie a
branch por cima — pergunte ao usuário o que fazer: pode ser um número errado (e nesse caso o
Passo 0 se repete com o número certo), ou pode ser intencional retomar uma branch que já existe
(nesse caso, `git checkout CARD-XX` em vez de criar, e avisar o usuário que não é uma branch nova).

## Passo 4 — Criar a branch

```
git checkout -b CARD-XX
```

Como a `Develop` já foi sincronizada no Passo 2, a branch nasce a partir do código mais atual.

## Passo 5 — Confirmar ao usuário

Rode `git branch --show-current` para confirmar que a branch nova está ativa, e avise o estado
final, por exemplo:

> Branch `CARD-XX` criada a partir da Develop atualizada. Pronta para começar o desenvolvimento.

Não é necessário publicar a branch no remoto agora (`git push`) — isso já acontece naturalmente
no Passo 2 do skill `finalizar-card`, quando o card for encerrado.

## Quando parar e perguntar

Pare o workflow automático e pergunte ao usuário se:
- A branch atual não é a `Develop` (ver Passo 1) — este é o caso mais comum e é intencional que o
  skill sempre pare aqui.
- O número do card não foi informado ou já existe como branch (local ou remota).
- `git pull` na Develop encontra conflitos.
- `git status` na branch atual (antes de trocar) mostra algo que parece merecer atenção antes de
  ser deixado para trás (ex.: mudanças pendentes não commitadas de um trabalho em andamento).
