FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RemoteControlServer/RemoteControlServer.csproj", "RemoteControlServer/"]
RUN dotnet restore "RemoteControlServer/RemoteControlServer.csproj"
COPY . .
WORKDIR "/src/RemoteControlServer"
RUN dotnet build "RemoteControlServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RemoteControlServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RemoteControlServer.dll"]
