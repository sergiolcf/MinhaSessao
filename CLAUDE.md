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
- Rodar localmente: dotnet run --project MinhaSessao/MinhaSessao.csproj
- Migrações EF Core: dotnet ef database update --project MinhaSessao/MinhaSessao.csproj

## Regras de Código
- Sempre mantenha os botões de confirmação/ação no canto inferior DIREITO dos modais.
- Um único formulário/modal de login para ambos os perfis (identificação via DB).
- Código e comentários em Português (PT-BR).
- Sempre validar alterações compilando a solução antes de finalizar tarefas.

## Segurança & Dependências
- NUNCA instale pacotes NuGet, bibliotecas externas ou serviços de terceiros sem solicitar autorização prévia ao usuário.
- O projeto DEVE utilizar exclusivamente tecnologias e bibliotecas 100% gratuitas e open-source (sem custos de licença ou planos pagos).
