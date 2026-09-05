# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura dependências
COPY ["*.sln", "./"]
COPY ["*.csproj", "./"]
RUN dotnet restore "MinhaSessao.csproj"

# Copia todo o resto do código e compila
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Estágio de Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Define a porta padrão da web
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MinhaSessao.dll"]