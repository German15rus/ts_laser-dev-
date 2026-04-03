FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY src/TsLaser.Crm.Api/TsLaser.Crm.Api.csproj src/TsLaser.Crm.Api/
RUN dotnet restore src/TsLaser.Crm.Api/TsLaser.Crm.Api.csproj

COPY src/ src/
RUN dotnet publish src/TsLaser.Crm.Api/TsLaser.Crm.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN mkdir -p /app/Data

COPY --from=build /app .
COPY src/TsLaser.Crm.Api/Templates/ ./Templates/
COPY src/TsLaser.Crm.Api/wwwroot/ ./wwwroot/

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TsLaser.Crm.Api.dll"]
