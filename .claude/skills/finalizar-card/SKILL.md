---
name: finalizar-card
description: Automatiza o workflow completo de git para encerrar um card de desenvolvimento no projeto MinhaSessao — compila a solução, atualiza o CLAUDE.md se necessário, commita as mudanças pendentes com um resumo simples, faz push da branch CARD-XX, faz merge (--no-ff) na branch Develop seguindo exatamente o padrão de mensagens já usado no histórico do repo, troca para a Develop e sincroniza com o remoto. Use este skill sempre que o usuário disser algo como "Finalizar este card", "finaliza esse card", "fechar o card", "terminei o card", "encerrar o card CARD-XX" ou pedir para concluir/fechar/dar merge no trabalho da branch atual — mesmo que não use exatamente essas palavras.
---

# Finalizar Card (MinhaSessao)

Workflow de git para fechar um card de desenvolvimento neste projeto, replicando exatamente o
padrão que já existe no histórico do repositório (veja `git log --graph`): uma branch `CARD-XX`
recebe um commit `feat: ... (CARD-XX)`, é enviada ao remoto, e depois é incorporada na `Develop`
com um commit de merge `merge: incorpora CARD-XX (...) em Develop`.

Este skill executa ações que afetam o histórico compartilhado (push, merge). O usuário já
autorizou esse fluxo especificamente ao pedir a criação deste skill — não é necessário perguntar
"posso fazer push?" a cada execução. Ainda assim, pare e avise o usuário sempre que algo fugir do
caminho esperado (ver seção "Quando parar e perguntar" no fim).

## Pré-requisito: precisa estar em uma branch CARD-XX

Rode `git branch --show-current`. Se a branch atual **não** for exatamente `CARD-<número>`
(ex.: `CARD-11`) — ou seja, se for `Develop`, `main`, `Homolog`, ou qualquer outro nome — **não
prossiga com o workflow**. Em vez disso:

1. Rode `git status` para checar se há mudanças pendentes (é comum o trabalho ter sido feito sem
   criar a branch antes, como pode acontecer neste projeto).
2. Explique ao usuário que o "Finalizar este card" exige uma branch `CARD-XX` (é assim que o
   histórico do projeto identifica cada card e monta a mensagem de merge).
3. Descubra o próximo número de card livre com `git branch -a | grep -oE 'CARD-[0-9]+' | sort -u`
   e sugira ao usuário criar a branch com `git checkout -b CARD-<próximo número>` — pergunte a ele
   se esse é o número certo antes de criar, já que o número do card é uma decisão dele, não algo
   que dá pra inferir do código.
4. Depois que o usuário confirmar/criar a branch correta, continue o workflow normalmente a partir
   do Passo 0.

Só siga adiante automaticamente quando a branch atual já for uma `CARD-XX` válida.

## Passo 0 — Build e CLAUDE.md

1. Rode `dotnet build SLCF_MinhaSessao.sln`. Esse é o mesmo comando que o CLAUDE.md do projeto já
   manda rodar antes de finalizar qualquer tarefa — não faz sentido commitar/mergear código que
   não compila. Se falhar, pare e resolva o erro (ou reporte ao usuário) antes de continuar.
2. Releia a seção "Arquitetura Implementada" do `CLAUDE.md` à luz do que mudou nesta branch
   (`git diff Develop...HEAD -- '*.cs' '*.cshtml'` ajuda a ver o que é novo). Se o card introduziu
   ou alterou um módulo (nova Controller/action, novo fluxo, nova regra) e isso ainda não está
   refletido no `CLAUDE.md`, atualize a seção correspondente agora — antes do commit, para que a
   documentação entre junto no mesmo commit do card.

## Passo 1 — Commit

1. Rode `git status` e `git diff` para ver exatamente o que está pendente. Se algo parecer fora de
   lugar (arquivo de credencial, `.env`, binário grande e inesperado), avise o usuário antes de
   continuar em vez de simplesmente incluir.
2. Se não houver nada para commitar, pule para o Passo 2.
3. Stage tudo que for do card (`git add -A` é esperado aqui — o objetivo deste passo é fechar o
   card inteiro) e crie um commit único no padrão já usado no projeto:

   ```
   feat: <resumo curto e específico do que o card faz> (CARD-XX)
   ```

   O resumo deve ser objetivo e descrever o que foi entregue (olhe o `git diff` e, se existir, o
   que foi atualizado no CLAUDE.md no Passo 0) — não é uma lista genérica de arquivos. Exemplos
   reais do próprio histórico: `feat: fluxo "Novo Paciente" por CPF, com CPF obrigatorio no
   cadastro (CARD-10)`, `feat: vinculo N:N entre Paciente e Profissional (CARD-08)`.

## Passo 2 — Push da branch do card

```
git push -u origin CARD-XX
```

(`-u` é seguro mesmo se o upstream já existir — só garante que vai existir caso essa seja a
primeira vez que a branch é publicada.)

## Passo 3 — Sincronizar e mergear na Develop

1. `git checkout Develop`
2. `git pull` — sincroniza a Develop local com o remoto **antes** do merge. Isso evita mergear em
   cima de uma Develop desatualizada e é o que garante que o merge seguinte não vai gerar
   conflitos evitáveis por falta de sync.
3. Merge com merge commit explícito (sem fast-forward), reaproveitando o mesmo resumo do commit
   `feat:` do Passo 1:

   ```
   git merge --no-ff CARD-XX -m "merge: incorpora CARD-XX (<mesmo resumo do commit feat>) em Develop"
   ```

   Se der conflito, resolva com cuidado olhando o código (não escolha um lado às cegas) e só
   finalize o merge depois de entender a divergência; se o conflito for grande ou ambíguo, pare e
   pergunte ao usuário antes de decidir.

## Passo 4 — Confirmar que está na Develop

Depois do merge, `git branch --show-current` já deve mostrar `Develop` (o `git checkout Develop`
do Passo 3 cuidou disso). Só confirme com `git status`.

## Passo 5 — Sync final da Develop (push)

Depois do merge, publique a Develop atualizada no remoto:

```
git push origin Develop
```

Esse é o "sync" completo: a `Develop` local ficou atualizada com o remoto antes do merge (Passo 3)
e agora o remoto fica atualizado com o merge que acabou de ser feito — ninguém mais precisa lembrar
de publicar manualmente depois. Termine avisando o estado final, por exemplo:

> Card CARD-XX finalizado: commit e push feitos na branch, merge feito na Develop e publicado no
> remoto (`git push origin Develop`).

## Quando parar e perguntar

Pare o workflow automático e pergunte ao usuário se:
- A branch atual não é uma `CARD-XX` válida (ver Pré-requisito acima).
- `dotnet build` falha e o motivo não é óbvio de corrigir.
- `git status`/`git diff` mostram algo que parece não pertencer ao card (credenciais, arquivos
  fora do escopo, mudanças que você não reconhece desta conversa).
- O merge gera conflitos não triviais.
- Já existem commits não sincronizados na Develop remota que sugerem que outra pessoa também está
  trabalhando nela (ex.: `git pull` no Passo 3 trouxe commits que não são deste card).
