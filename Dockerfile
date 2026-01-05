# ЕТАП 1: Збірка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копіюємо .sln та всі .csproj для кешування NuGet
COPY *.sln ./
COPY JobVacancyCollector/*.csproj ./JobVacancyCollector/
COPY JobVacancyCollector.Application/*.csproj ./JobVacancyCollector.Application/
COPY JobVacancyCollector.Domain/*.csproj ./JobVacancyCollector.Domain/
COPY JobVacancyCollector.Infrastructure/*.csproj ./JobVacancyCollector.Infrastructure/
COPY JobVacancyCollector.Worker/*.csproj ./JobVacancyCollector.Worker/

RUN dotnet restore

# Копіюємо все інше
COPY . ./

# Публікуємо саме точку входу
RUN dotnet publish JobVacancyCollector/JobVacancyCollector.csproj -c Release -o out

# ЕТАП 2: Запуск
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Важливо для контейнеризації
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "JobVacancyCollector.dll"]