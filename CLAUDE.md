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
- Um único formulário/modal de login para ambos os perfis (identificação via DB).
- Código e comentários em Português (PT-BR).
- Sempre validar alterações compilando a solução antes de finalizar tarefas.

## Segurança & Dependências
- NUNCA instale pacotes NuGet, bibliotecas externas ou serviços de terceiros sem solicitar autorização prévia ao usuário.
- O projeto DEVE utilizar exclusivamente tecnologias e bibliotecas 100% gratuitas e open-source (sem custos de licença ou planos pagos).

## CARD-03: Cadastro de Profissional e Dashboard (Implementado)
- Namespace raiz do projeto é `MinhaSessao` (não `SLCF_MinhaSessao` — este é só o nome do arquivo `.sln`). Toda a estrutura de Models usa `MinhaSessao.Models.*`.
- Models divididos por responsabilidade:
  - `Models/Entities/Profissional.cs` — entidade mapeada pelo EF Core (`Id` como `Guid`, gerado via `Guid.NewGuid()`, `FotoUrl` string).
  - `Models/ViewModels/ProfissionalViewModel.cs` — model do formulário (`Id` também `Guid`, campo `Foto` como `IFormFile` para upload, não persistido diretamente).
- Persistência via EF Core + SQLite: `Data/ApplicationDbContext.cs` (`DbSet<Profissional> Profissionais`), connection string em `appsettings.json` (`ConnectionStrings:DefaultConnection`), migration `CriarTabelaProfissional` já aplicada ao banco local (`minhasessao.db`, gitignored).
- `Controllers/ProfissionalController.cs`: action `[HttpPost] Criar(ProfissionalViewModel model)`, protegida com `[ValidateAntiForgeryToken]`, injeta `ApplicationDbContext` e `IWebHostEnvironment`. Faz upload da foto (se enviada) para `wwwroot/uploads/profissionais/` com nome único (Guid + extensão), cria a pasta dinamicamente se não existir, salva a entidade em `try/catch` e sempre retorna JSON (`success`, `message`, `errors` quando inválido).
- View (`Views/Home/_CadastroProfissionalModal.cshtml`) usa Tag Helpers (`asp-for`, `asp-validation-for`) ligados à `ProfissionalViewModel`.
- Feedback visual via JavaScript nativo (Fetch API, sem jQuery/AJAX de terceiros):
  - Botão "Criar Conta" desabilita e mostra spinner Bootstrap durante o envio.
  - Sucesso: fecha a modal do formulário e abre `#modalSucessoCadastro` (ícone de check verde `#198754`, "Cadastrado com Sucesso!").
  - Erro: mantém a modal do formulário aberta com os dados preenchidos, exibe alerta vermelho (`#DC3545`, ícone de X) com a mensagem específica vinda da Controller/ModelState, reabilita o botão.
- Pacotes NuGet instalados (autorização já concedida pelo usuário): `Microsoft.EntityFrameworkCore.Sqlite` e `Microsoft.EntityFrameworkCore.Design`, fixados em `8.0.10` (compatibilidade com `net8.0`).
- Pós-cadastro, `ProfissionalController.Criar` retorna `redirectUrl` (`/Dashboard?profissionalId={id}`) no JSON de sucesso; o botão "Continuar" do modal de sucesso (`wwwroot/js/site.js`) redireciona o navegador para essa URL.

### Login Simulado / Dashboard do Profissional
- `Controllers/DashboardController.cs`: action `Index(Guid? profissionalId)` busca o profissional pelo id informado ou, se omitido, o primeiro cadastrado no banco (facilita o desenvolvimento sem autenticação real ainda). Se nenhum profissional existir, redireciona para `Home/Index`. Expõe `ProfissionalId`, `ProfissionalNome`, `ProfissionalFotoUrl` e `ProfissionalCRP` via `ViewBag` (consumidos pelo layout) e retorna `Models/ViewModels/DashboardViewModel.cs` como model da view.
- `Views/Shared/_LayoutDashboard.cshtml`: layout próprio do painel interno (não usa o `_Layout.cshtml` da landing page pública), com navbar e sidebar na cor grafite (`#2B2D42`) e destaques em laranja (`#FF6B35`). Bootstrap Icons via CDN (`cdn.jsdelivr.net/npm/bootstrap-icons`). Avatar do profissional em `rounded-circle` 40x40 com fallback de ícone neutro (`bi-person-fill`) quando `FotoUrl` é nulo/vazio.
- Sidebar retrátil: botão hambúrguer alterna a classe `.sidebar-collapsed` no `#dashSidebar` (largura 250px → 70px, `transition: all 0.3s ease`, oculta `.menu-text` e centraliza os ícones); estado persistido em `localStorage` (`wwwroot/js/dashboard.js`, chave `msSidebarCollapsed`). Sem preferência salva, a sidebar inicia recolhida em telas ≤768px e expandida em telas maiores.
- Estado ativo (`.active`) do menu é calculado dinamicamente por rota (`ViewContext.RouteData` — controller/action atuais), nunca fixo no HTML. O item "Início" (topo da sidebar, ícone `bi-grid-1x2`) aponta para `Dashboard/Index`; todos os links reais da sidebar e a logo do navbar usam Tag Helpers (`asp-route-profissionalId`) para preservar o profissional "logado" (sessão simulada, sem autenticação real) durante a navegação.
- Demais itens da sidebar (Minha Agenda, Meus Pacientes, Minhas Sessões, Configurações) ainda são placeholders (`href="#"`) — controllers/actions reais serão implementados em cards futuros, seguindo o mesmo padrão de link com `asp-route-profissionalId`.
- Estilos em `wwwroot/css/dashboard.css` e script em `wwwroot/js/dashboard.js`, isolados do `site.css`/`site.js` da landing page pública.
