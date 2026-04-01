# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ResimamisBackend.csproj .
RUN dotnet restore ResimamisBackend.csproj

COPY . .
RUN dotnet publish ResimamisBackend.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render inyecta PORT en runtime; Program.cs usa UseUrls con ese valor.
EXPOSE 8080

ENTRYPOINT ["dotnet", "ResimamisBackend.dll"]
