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
- Toda action que lê/grava dados vinculados a um Profissional (Pacientes, Anotações, etc.) deve usar `[Authorize(Roles = AutenticacaoService.PapelProfissional)]` e obter o Id do profissional logado via `User.ObterProfissionalId()`; toda action exclusiva do Paciente usa `[Authorize(Roles = AutenticacaoService.PapelPaciente)]` e `User.ObterPacienteId()` — nunca confiar em um Id vindo de querystring, rota ou corpo da requisição para autorização.
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

### Autenticação (Cookie + Senha, Profissional e Paciente)
- Autenticação real por Cookie (`Program.cs`: `AddAuthentication().AddCookie()`, `LoginPath = /Account/Login`, expira em 7 dias com sliding expiration).
- Senha nunca é armazenada em texto puro: hash via `PasswordHasher<Profissional>`/`PasswordHasher<Paciente>` (`Microsoft.AspNetCore.Identity`, já incluso no SDK `Microsoft.NET.Sdk.Web`, sem pacote NuGet adicional).
- `Services/AutenticacaoService.cs` centraliza hash/verificação de senha (`HashSenha`/`VerificarSenha` para Profissional, `HashSenhaPaciente`/`VerificarSenhaPaciente` para Paciente) e a autenticação (`AutenticarProfissionalAsync`/`AutenticarPacienteAsync`, montam os Claims e chamam `SignInAsync`). Ambos os métodos de autenticação adicionam um Claim de papel (`ClaimTypes.Role`) com o valor de `AutenticacaoService.PapelProfissional` ou `PapelPaciente` — é isso que permite `[Authorize(Roles = "...")]` diferenciar as rotas de cada perfil.
- `Extensions/ClaimsPrincipalExtensions.cs`: `ObterProfissionalId()`/`ObterPacienteId()` extraem o Id do usuário logado a partir dos Claims — é assim que toda controller descobre "quem está logado" (cada uma só é chamada dentro de controllers já restritas ao papel correspondente).
- `Controllers/AccountController.cs`: `Login` (GET/POST) é o único formulário para os dois perfis — normaliza o e-mail com `.Trim().ToLower()`, procura primeiro em `Profissionais` e depois em `Pacientes`, autentica com o método correspondente e redireciona para `Dashboard` (Profissional) ou `PainelPaciente` (Paciente). `LoginSimuladoTeste` (atalho de desenvolvimento, loga automaticamente o primeiro profissional do banco), `Logout` (comum aos dois perfis).
- Views: `Views/Account/Login.cshtml` com `Views/Shared/_LayoutAuth.cshtml` (layout minimalista, não usa o `_Layout.cshtml` público nem os layouts de dashboard).
- Ainda não implementado: troca de senha obrigatória no primeiro login, recuperação de senha e autocadastro do Paciente (ele só existe hoje via cadastro feito pelo Profissional, com a senha temporária descrita em "Pacientes" abaixo).

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
- `Controllers/PacientesController.cs`: `Index` (lista os pacientes vinculados ao profissional logado), `Detalhes` (ficha do paciente), `Criar`, `GerarNovaSenha`.
- Toda consulta/gravação revalida a posse via `VinculoService` (ver seção "Vínculo Paciente-Profissional" abaixo) — **nunca** compara um `ProfissionalId` direto na entidade `Paciente` (ela não tem mais esse campo).
- Acesso do paciente: ao cadastrar, `Criar` gera uma senha temporária aleatória (`AutenticacaoService.GerarSenhaTemporaria`), salva só o hash (`HashSenhaPaciente`) e retorna o texto puro **uma única vez** no JSON de sucesso. O front-end (`wwwroot/js/pacientes.js` + modal compartilhada `#modalSenhaTemporaria` em `_LayoutDashboard.cshtml`, via `window.exibirSenhaTemporaria`) exibe essa senha com um botão de copiar (`navigator.clipboard`) — ela nunca é armazenada em texto puro nem pode ser recuperada depois.
- Se a senha for perdida, o profissional usa o botão "Gerar nova senha" na aba Dados Cadastrais da ficha (`wwwroot/js/pacientes-detalhes.js`, action `GerarNovaSenha`), que sobrescreve o hash antigo (invalidando-o) e mostra a nova senha do mesmo jeito. Não existe fluxo de "recuperar" a senha antiga — só resetar.
- O paciente já consegue logar com essa senha em `/Account/Login` (ver seção "Autenticação" acima) e cai no seu próprio painel — ver "Painel do Paciente" abaixo.

### Vínculo Paciente-Profissional
- Relação N:N via `Models/Entities/VinculoPacienteProfissional.cs` (`PacienteId`, `ProfissionalId`, `Status` — enum `StatusVinculo.Ativo`/`Encerrado`, `DataInicio`, `DataFim`) — substituiu a antiga FK direta `Paciente.ProfissionalId` (removida), permitindo que um paciente tenha vários profissionais ao longo do tempo.
- `Services/VinculoService.cs` (registrado no DI como scoped) centraliza toda a checagem de posse e criação de vínculo: `PacientePertenceAoProfissionalAsync(pacienteId, profissionalId)` (só considera vínculo com `Status = Ativo`), `ObterPacientesAtivosAsync(profissionalId)` (lista para o `Index`) e `CriarVinculo(pacienteId, profissionalId)` (usado por `PacientesController.Criar`; não chama `SaveChangesAsync` sozinho — quem chama inclui na mesma unidade de trabalho que grava o `Paciente`).
- Nenhuma controller compara `ProfissionalId` direto na entidade `Paciente` — toda checagem de posse (Pacientes e Anotações Confidenciais) passa por `VinculoService`.
- Migration `AddVinculoPacienteProfissional`: cria a tabela `Vinculos` **antes** de mexer em `Pacientes`, faz um `INSERT ... SELECT` migrando cada paciente existente para um vínculo `Ativo` (com a `DataCadastro` do paciente como `DataInicio`) e só depois remove a FK/coluna antiga — a ordem importa, senão os dados são perdidos antes de serem copiados.
- Ainda não existe tela de gerenciar vínculos (ex.: encerrar vínculo com um paciente) — fica para um card futuro.

### Painel do Paciente
- Autenticação e diferenciação de papel (Profissional vs Paciente) descritas na seção "Autenticação" acima.
- `Controllers/PainelPacienteController.cs` (`[Authorize(Roles = AutenticacaoService.PapelPaciente)]`): action `Index` — obtém o Id do paciente via `User.ObterPacienteId()`, carrega o próprio registro e exibe a tela "Início" (boas-vindas + cards placeholder).
- Layout próprio `Views/Shared/_LayoutDashboardPaciente.cshtml` — **não** reaproveita o `_LayoutDashboard.cshtml` do Profissional (layouts separados por perfil, mesmo padrão de isolamento já usado no restante do projeto), mas reaproveita `wwwroot/css/dashboard.css` e `wwwroot/js/dashboard.js` (sidebar retrátil, item ativo por rota — mesmo comportamento do dashboard do Profissional).
- Sidebar do Paciente: "Início" (real), "Minhas Sessões", "Meu(s) Psicólogo(s)" e "Configurações" (essas três ainda são placeholders `href="#"`, sem controller/action própria).
- Views em `Views/PainelPaciente/*`, model `Models/ViewModels/PainelPacienteViewModel.cs`.

### Badge "Alfa"
- `Views/Shared/_BadgeAlfa.cshtml` — partial isolado, incluído ao lado do logo em `Views/Home/Index.cshtml`, `_LayoutDashboard.cshtml` e `_LayoutDashboardPaciente.cshtml`.
- Feito para ser descartado facilmente quando o projeto sair da fase Alfa: basta apagar o arquivo e as 3 referências `<partial name="_BadgeAlfa" />`.

### Anotações Confidenciais (dentro da Ficha do Paciente)
- Entidade `Models/Entities/AnotacaoConfidencial.cs`, vinculada a `Paciente` e `Profissional`.
- CRUD completo via AJAX, sem reload de página: `SalvarAnotacao`, `AtualizarAnotacao`, `ExcluirAnotacao`, `BuscarAnotacoes` (paginação de 10 por página + busca por título + ordenação por data), `SugerirTitulosAnotacao` (autocomplete, sempre as 3 mais recentes que dão match).
- Timeline visual com scroll interno (`.ms-timeline-scroll`, ~4 itens visíveis por vez) e paginação Bootstrap abaixo.
- Toda action verifica que a anotação/paciente pertence ao profissional logado antes de ler ou gravar.
