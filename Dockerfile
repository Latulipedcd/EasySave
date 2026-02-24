FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copier tout le projet
COPY . .

# Restaurer depuis le bon chemin
RUN dotnet restore LogServer/LogServer.csproj

# Build
RUN dotnet publish LogServer/LogServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "LogServer.dll"]