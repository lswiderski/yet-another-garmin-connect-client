FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Default in ASP.NET 8 image is 8080
EXPOSE 8080
# If you really want port 80 instead, uncomment:
# ENV ASPNETCORE_URLS=http://+:80
# EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# copy only csproj first (better restore cache)
COPY Api.csproj .
RUN dotnet restore Api.csproj

# then copy the rest
COPY . .
RUN dotnet publish Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
