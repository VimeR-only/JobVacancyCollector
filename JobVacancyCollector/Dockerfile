FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JobVacancyCollector/JobVacancyCollector.csproj", "JobVacancyCollector/"]
COPY ["JobVacancyCollector.Application/JobVacancyCollector.Application.csproj", "JobVacancyCollector.Application/"]
COPY ["JobVacancyCollector.Domain/JobVacancyCollector.Domain.csproj", "JobVacancyCollector.Domain/"]
COPY ["JobVacancyCollector.Infrastructure/JobVacancyCollector.Infrastructure.csproj", "JobVacancyCollector.Infrastructure/"]
COPY ["JobVacancyCollector.Worker/JobVacancyCollector.Worker.csproj", "JobVacancyCollector.Worker/"]
RUN dotnet restore "./JobVacancyCollector/JobVacancyCollector.csproj"
COPY . .
WORKDIR "/src/JobVacancyCollector"
RUN dotnet build "./JobVacancyCollector.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobVacancyCollector.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobVacancyCollector.dll"]