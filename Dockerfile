FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
COPY ./IPWatcher/ /src
WORKDIR /src
RUN dotnet restore . 
RUN dotnet build ./src/IPWatcher.SyncService/ -c Release -o /app/build

RUN dotnet test ./test/IpWatcher.Tests/

FROM build-env AS publish
RUN dotnet publish ./src/IPWatcher.SyncService/ -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","IPWatcher.SyncService.dll"]