# ─── build ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore RuleForge.Api/RuleForge.Api.csproj && \
    dotnet publish RuleForge.Api/RuleForge.Api.csproj \
      -c Release \
      --no-restore \
      -o /app/publish

# ─── migrate ──────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS migrate
WORKDIR /src

COPY . .

RUN dotnet restore RuleForge.Api/RuleForge.Api.csproj && \
    dotnet tool install --global dotnet-ef --version 8.*

ENV PATH="$PATH:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "ef", "database", "update", \
            "--project", "RuleForge.Infrastructure", \
            "--startup-project", "RuleForge.Api", \
            "--no-build"]

# ─── runtime ──────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "RuleForge.Api.dll"]
