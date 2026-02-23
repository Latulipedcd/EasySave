# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copier toute la solution
COPY . .

# Restaurer depuis la solution
RUN dotnet restore EasySave.sln

# Build publication
RUN dotnet publish LogServer/LogServer.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "LogServer.dll"]