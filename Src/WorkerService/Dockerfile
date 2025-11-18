FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Kopiujemy plik zarzadzania wersjami (musi byc obok folderow z projektami w kontenerze)
COPY ["Directory.Packages.props", "."]

# 2. Kopiujemy caly kod zrodlowy (potrzebne, bo WorkerService zalezy od Application/Domain/etc.)
COPY Src ./Src

# 3. Przechodzimy do folderu projektu, ktory chcemy zbudowac
WORKDIR "/src/Src/WorkerService"

# 4. Restore i Build
RUN dotnet restore "WorkerService.csproj"
RUN dotnet build "WorkerService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WorkerService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkerService.dll"]