FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
COPY . /src
WORKDIR /src
RUN dotnet restore
RUN dotnet build ./IPWatcher.SyncService/ -c Release -o /app/build

FROM build-env AS publish
RUN dotnet publish ./IPWatcher.SyncService/ -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","IPWatcher.SyncService.dll"]
