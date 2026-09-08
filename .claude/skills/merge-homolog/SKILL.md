---
name: merge-homolog
description: Automatiza o merge da branch Develop na Homolog neste projeto MinhaSessao, com duas checagens de segurança específicas desse ambiente compartilhado — garante que o botão "Entrar com Conta de Teste" nunca fique visível em Homolog (confere a flag ExibirLoginContaTeste, independente de ASPNETCORE_ENVIRONMENT) e garante que nenhum código temporário de carga de dados de teste (endpoints "Seed*"/comentários "TEMPORÁRIO") seja promovido pra lá, já que cada ambiente tem seus próprios registros e nunca devem ser copiados/gerados de um pro outro. Use este skill sempre que o usuário disser algo como "merge pra homolog", "sobe pra homolog", "atualiza a homolog", "manda pra homolog", "promove a develop pra homolog", "sincroniza homolog" ou pedir para publicar/levar o trabalho da Develop pro ambiente de Homologação — mesmo que não use exatamente essas palavras.
---

# Merge para Homolog (MinhaSessao)

Workflow de git para promover a `Develop` pra `Homolog` neste projeto. Diferente do
[finalizar-card](../finalizar-card/SKILL.md) (que fecha um card individual na Develop), aqui o
merge é sempre **Develop → Homolog** — Homolog não recebe branches `CARD-XX` diretamente.

Homolog é um ambiente compartilhado e hospedado (Render) que qualquer pessoa pode abrir a
qualquer momento pra testar — por isso existem duas checagens específicas dele, feitas **antes**
do merge, que não existem no fluxo de fechar um card:

## Checagem 1 — o botão "Entrar com Conta de Teste" não pode vazar pra lá

Esse botão (`Views/Account/Login.cshtml`) é controlado pela configuração `ExibirLoginContaTeste`
— **não** por `ASPNETCORE_ENVIRONMENT` (isso já foi uma fonte de bug real: um serviço hospedado
com `ASPNETCORE_ENVIRONMENT=Development` por qualquer motivo, como o próprio Homolog, acabava
expondo esse atalho de login sem querer). O default em `appsettings.json` é `false` — vale pra
qualquer ambiente hospedado que não sobrescreva explicitamente — e só fica `true` localmente via
`Properties/launchSettings.json`, arquivo que nenhum serviço no Render lê.

Antes do merge, confirme que essa proteção continua intacta na `Develop`:

```
git show Develop:Views/Account/Login.cshtml | grep -n "ExibirLoginContaTeste"
git show Develop:appsettings.json | grep -n "ExibirLoginContaTeste"
```

Espera-se encontrar `Configuration.GetValue<bool>("ExibirLoginContaTeste")` na View e
`"ExibirLoginContaTeste": false` no `appsettings.json`. Se algum dos dois não aparecer (alguém
reintroduziu `Env.IsDevelopment()`, ou mudou o default pra `true`), **pare e avise o usuário** —
não prossiga com o merge sem entender por que a proteção mudou.

Mesmo com o código correto, a visibilidade final depende de o serviço Homolog no Render **não**
ter uma variável de ambiente `ExibirLoginContaTeste=true` configurada — isso é um ajuste no painel
do Render, fora do alcance do git. Sempre termine o skill lembrando o usuário de conferir isso se o
botão ainda aparecer depois do deploy.

## Checagem 2 — nenhuma carga de dados de teste pode ir junto

Cada ambiente (Develop, Homolog, Produção) tem seu próprio banco com seus próprios registros —
pacientes, sessões, anotações reais daquele ambiente. Um merge de código **nunca** move linhas de
banco de dados (isso não é algo que o git faz), mas o risco real é diferente: se alguma vez ficar
esquecido no código um endpoint temporário de carga de teste (o padrão usado neste projeto até
agora: uma action em `AccountController.cs` comentada como `// TEMPORÁRIO (remover após uso)`,
geralmente chamada `Seed*`), promovê-lo pra Homolog significa que qualquer pessoa com acesso à URL
pode gerar dados fictícios lá — poluindo um ambiente que deveria refletir dados de teste
controlados, revisados, ou já usados por outra pessoa.

Antes do merge, confira se sobrou algum resquício assim na `Develop`:

```
git grep -n "TEMPORÁRIO\|TEMPORARIO" Develop -- '*.cs'
git grep -n "public async Task<IActionResult> Seed" Develop -- '*.cs'
```

Se aparecer alguma linha, **pare e avise o usuário** — esse código precisa ser removido da
`Develop` (mesmo padrão de limpeza já usado neste projeto: implementar a rota, confirmar que
funcionou, depois apagar o método inteiro) antes de continuar. Nunca decida sozinho apagar isso
durante o merge — avise e deixe o usuário confirmar.

**O que não entra nessa checagem**: migrations do EF Core que alteram apenas *schema* (nova
coluna, nova tabela, um backfill que só preenche um valor calculado numa coluna nova — ex.: a
migration `AdicionaCodigoSessao`) são esperadas e **devem** rodar em todo ambiente via
`context.Database.Migrate()` (chamado automaticamente no `Program.cs` a cada início da aplicação)
— isso é sincronizar a estrutura do banco, não copiar registros de negócio de um ambiente pro
outro. A checagem acima é só sobre dados fictícios/de teste, não sobre estrutura de tabela.

## Passos do merge

Só prossiga pra cá depois que as duas checagens acima passarem (ou o usuário explicitamente disser
pra seguir mesmo assim).

1. `git status` — confirme que não há mudanças pendentes não commitadas que deveriam ir num card
   separado antes. Se houver algo relevante, avise o usuário em vez de simplesmente incluir.
2. `dotnet build SLCF_MinhaSessao.sln` a partir da `Develop` — não faz sentido promover código que
   não compila pra um ambiente que outras pessoas vão testar.
3. `git checkout Homolog`
4. `git pull` — sincroniza a Homolog local com o remoto antes do merge.
5. `git merge --no-ff Develop -m "merge: atualiza Homolog com a Develop (<resumo do que está sendo promovido>)"`
   — merge commit explícito, sem fast-forward, mesmo espírito do merge de card na Develop. Se der
   conflito, resolva com cuidado olhando o código (nunca escolha um lado às cegas); se for grande
   ou ambíguo, pare e pergunte ao usuário.
6. `dotnet build SLCF_MinhaSessao.sln` de novo, já na Homolog mergeada — confirma que o resultado
   do merge compila antes de publicar.
7. `git push origin Homolog`
8. `git checkout Develop` — deixa o repositório de volta na branch de trabalho padrão.

Termine avisando o estado final e lembrando o item que não dá pra automatizar, por exemplo:

> Homolog atualizada com a Develop e publicada no remoto. Só lembrando: confirma no painel do
> Render que o serviço de Homolog não tem `ExibirLoginContaTeste=true` configurado, senão o botão
> de teste aparece mesmo com o código correto.

## Quando parar e perguntar

- Alguma das duas checagens de segurança falhar (proteção do botão de teste ausente/alterada, ou
  resquício de endpoint `Seed*`/`TEMPORÁRIO` encontrado).
- `dotnet build` falhar (antes ou depois do merge) e o motivo não for óbvio de corrigir.
- `git status` mostrar mudanças pendentes que não parecem pertencer a este merge.
- O merge gerar conflitos não triviais.
- `git pull` na Homolog trouxer commits que não vieram da Develop (sinal de que alguém commitou
  direto na Homolog, fora do fluxo normal).
