# MinhaSessao - Diretrizes do Projeto

## Visão Geral
Sistema de gestão de sessões de psicologia para Psicólogos e Pacientes.
Solução: SLCF_MinhaSessao.sln
Tech Stack: .NET 8 MVC, Entity Framework Core, SQLite, Bootstrap 5.

## Paleta de Cores & Layout
- Fundo Principal: Cinza Claro (#F8F9FA)
- Texto / Elementos Primários: Grafite (#2B2D42)
- Destaques / Botões de Ação: Laranja (#FF6B35)
- Layout: Limpo, acolhedor, com uso de Pop-ups (Modais Bootstrap) para autenticação.

## Comandos Principais
- Compilar Solução: dotnet build SLCF_MinhaSessao.sln
- Rodar localmente: dotnet run --project MinhaSessao.csproj
- Migrações EF Core: dotnet ef database update --project MinhaSessao.csproj

## Regras de Código
- Sempre mantenha os botões de confirmação/ação no canto inferior DIREITO dos modais.
- Um único formulário de login para ambos os perfis (identificação via DB) — hoje é a página `/Account/Login`, não mais um modal.
- Código e comentários em Português (PT-BR).
- Sempre validar alterações compilando a solução antes de finalizar tarefas.
- Toda action que lê/grava dados vinculados a um Profissional (Pacientes, Anotações, etc.) deve usar `[Authorize]` e obter o Id do profissional logado via `User.ObterProfissionalId()` — nunca confiar em um Id vindo de querystring, rota ou corpo da requisição para autorização.
- Operações de CRUD dentro do Dashboard (criar/editar/excluir/paginar/buscar) devem ser feitas via Fetch API (AJAX), sem recarregar a página inteira.

## Segurança & Dependências
- NUNCA instale pacotes NuGet, bibliotecas externas ou serviços de terceiros sem solicitar autorização prévia ao usuário.
- O projeto DEVE utilizar exclusivamente tecnologias e bibliotecas 100% gratuitas e open-source (sem custos de licença ou planos pagos).

## Arquitetura Implementada
Esta seção descreve o estado atual do projeto por módulo/domínio (não por card). Ao implementar um novo card que altera um módulo existente, atualize a seção correspondente em vez de adicionar uma seção nova.

### Estrutura de Models e Projeto
- Namespace raiz do projeto é `MinhaSessao` (não `SLCF_MinhaSessao` — este é só o nome do arquivo `.sln`).
- `Models/Entities/*` — entidades mapeadas pelo EF Core (`Id` como `Guid`, gerado via `Guid.NewGuid()`).
- `Models/ViewModels/*` — models de formulário/exibição, um por finalidade (criação, edição, listagem, detalhes).
- `Services/*` — lógica de domínio reutilizável (ex.: `AutenticacaoService`).
- `Extensions/*` — extension methods auxiliares (ex.: `ClaimsPrincipalExtensions`).
- Persistência via EF Core + SQLite: `Data/ApplicationDbContext.cs`, connection string em `appsettings.json` (`ConnectionStrings:DefaultConnection`), banco local `minhasessao.db` (gitignored).

### Autenticação (Cookie + Senha)
- Autenticação real por Cookie (`Program.cs`: `AddAuthentication().AddCookie()`, `LoginPath = /Account/Login`, expira em 7 dias com sliding expiration).
- Senha nunca é armazenada em texto puro: hash via `PasswordHasher<Profissional>` (`Microsoft.AspNetCore.Identity`, já incluso no SDK `Microsoft.NET.Sdk.Web`, sem pacote NuGet adicional).
- `Services/AutenticacaoService.cs` centraliza `HashSenha`, `VerificarSenha` e `AutenticarProfissionalAsync` (monta os Claims e chama `SignInAsync`).
- `Extensions/ClaimsPrincipalExtensions.ObterProfissionalId()` extrai o Id do profissional logado a partir dos Claims — é assim que toda controller descobre "quem está logado".
- `Controllers/AccountController.cs`: `Login` (GET/POST, normaliza e-mail com `.Trim().ToLower()`), `LoginSimuladoTeste` (atalho de desenvolvimento, loga automaticamente o primeiro profissional do banco), `Logout`.
- Views: `Views/Account/Login.cshtml` com `Views/Shared/_LayoutAuth.cshtml` (layout minimalista, não usa o `_Layout.cshtml` público nem o `_LayoutDashboard.cshtml`).

### Cadastro de Profissional
- `Views/Home/_CadastroProfissionalModal.cshtml` — modal na landing page pública, Tag Helpers (`asp-for`, `asp-validation-for`) ligados à `ProfissionalViewModel` (inclui `Senha`/`ConfirmarSenha`).
- `Controllers/ProfissionalController.cs` (`[HttpPost] Criar`, protegida com `[ValidateAntiForgeryToken]`): faz upload da foto opcional para `wwwroot/uploads/profissionais/` (nome único via Guid), salva o profissional com e-mail normalizado e senha já hasheada, sempre retorna JSON (`success`, `message`, `errors`).
- Após cadastro bem-sucedido, autentica automaticamente (Cookie) e retorna `redirectUrl` para o Dashboard; feedback visual (spinner, modal de sucesso/erro) via `wwwroot/js/site.js`, sem jQuery/AJAX de terceiros.

### Dashboard
- `Views/Shared/_LayoutDashboard.cshtml`: layout do painel interno, navbar/sidebar em grafite (`#2B2D42`) com destaques em laranja (`#FF6B35`), Bootstrap Icons via CDN.
- Sidebar retrátil (botão hambúrguer alterna `.sidebar-collapsed`), estado persistido em `localStorage` (`wwwroot/js/dashboard.js`); item ativo do menu calculado dinamicamente pela rota atual (`ViewContext.RouteData`), nunca fixo no HTML.
- Itens "Minha Agenda", "Minhas Sessões" e "Configurações" ainda são placeholders (`href="#"`) — sem controller/action própria até o momento.
- Estilos/scripts isolados em `wwwroot/css/dashboard.css` e `wwwroot/js/dashboard.js` (não compartilham `site.css`/`site.js` da landing page).

### Pacientes
- `Controllers/PacientesController.cs`: `Index` (lista os pacientes do profissional logado), `Detalhes` (ficha do paciente), `Criar`, `GerarNovaSenha`.
- Toda consulta/gravação revalida que o paciente pertence ao `ProfissionalId` do usuário logado.
- Acesso do paciente: ao cadastrar, `Criar` gera uma senha temporária aleatória (`AutenticacaoService.GerarSenhaTemporaria`), salva só o hash (`HashSenhaPaciente`) e retorna o texto puro **uma única vez** no JSON de sucesso. O front-end (`wwwroot/js/pacientes.js` + modal compartilhada `#modalSenhaTemporaria` em `_LayoutDashboard.cshtml`, via `window.exibirSenhaTemporaria`) exibe essa senha com um botão de copiar (`navigator.clipboard`) — ela nunca é armazenada em texto puro nem pode ser recuperada depois.
- Se a senha for perdida, o profissional usa o botão "Gerar nova senha" na aba Dados Cadastrais da ficha (`wwwroot/js/pacientes-detalhes.js`, action `GerarNovaSenha`), que sobrescreve o hash antigo (invalidando-o) e mostra a nova senha do mesmo jeito. Não existe fluxo de "recuperar" a senha antiga — só resetar.
- Login do próprio paciente com essa senha ainda não foi implementado (fora de escopo até agora); por enquanto a senha existe só para ser repassada manualmente ao paciente.

### Anotações Confidenciais (dentro da Ficha do Paciente)
- Entidade `Models/Entities/AnotacaoConfidencial.cs`, vinculada a `Paciente` e `Profissional`.
- CRUD completo via AJAX, sem reload de página: `SalvarAnotacao`, `AtualizarAnotacao`, `ExcluirAnotacao`, `BuscarAnotacoes` (paginação de 10 por página + busca por título + ordenação por data), `SugerirTitulosAnotacao` (autocomplete, sempre as 3 mais recentes que dão match).
- Timeline visual com scroll interno (`.ms-timeline-scroll`, ~4 itens visíveis por vez) e paginação Bootstrap abaixo.
- Toda action verifica que a anotação/paciente pertence ao profissional logado antes de ler ou gravar.
