FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["JobVacancyCollector.Worker/JobVacancyCollector.Worker.csproj", "JobVacancyCollector.Worker/"]
COPY ["JobVacancyCollector.Application/JobVacancyCollector.Application.csproj", "JobVacancyCollector.Application/"]
COPY ["JobVacancyCollector.Domain/JobVacancyCollector.Domain.csproj", "JobVacancyCollector.Domain/"]
COPY ["JobVacancyCollector.Infrastructure/JobVacancyCollector.Infrastructure.csproj", "JobVacancyCollector.Infrastructure/"]

RUN dotnet restore "JobVacancyCollector.Worker/JobVacancyCollector.Worker.csproj"

COPY . .
WORKDIR "/src/JobVacancyCollector.Worker"

RUN dotnet build "JobVacancyCollector.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "JobVacancyCollector.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobVacancyCollector.Worker.dll"]