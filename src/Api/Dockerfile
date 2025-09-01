# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# ASP.NET 8 listens on 8080 by default
EXPOSE 8080
# If you prefer port 80 instead, uncomment the next two lines:
# ENV ASPNETCORE_URLS=http://+:80
# EXPOSE 80

# Build / publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# === Restore (cache-friendly) ===
# Copy only csproj files first so restore can be cached
COPY Api/Api.csproj Api/
COPY YetAnotherGarminConnectClient/YetAnotherGarminConnectClient.csproj YetAnotherGarminConnectClient/

RUN dotnet restore Api/Api.csproj

# === Build ===
# Now copy the rest of the source
COPY . .

WORKDIR /src/Api
RUN dotnet publish Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# === Final image ===
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
