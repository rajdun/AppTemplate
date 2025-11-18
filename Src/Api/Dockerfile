FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y libicu-dev && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy the Package Management file to the root of the build context
COPY ["Directory.Packages.props", "."]

# 2. Copy the ENTIRE Src folder to ./Src
# This is critical! It preserves the structure so Api can find Domain/Application
COPY Src ./Src

# 3. Switch context to the Api project folder
WORKDIR "/src/Src/Api"

# 4. Restore and Build
# The restore command will automatically look up the directory tree 
# and find Directory.Packages.props in /src
RUN dotnet restore "Api.csproj"

# (Optional) Only uncomment if you copied the .config folder and really need tools
# RUN dotnet tool restore 

RUN dotnet build "Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]