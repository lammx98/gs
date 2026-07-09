# Build context: repository root (c:\Source\GS)
#   docker build -f GS.TenantService/Dockerfile -t gs-tenant-service .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GS.Core/GS.Core.csproj GS.Core/
COPY GS.MultiTenant/GS.MultiTenant.csproj GS.MultiTenant/
COPY GS.TenantService/GS.TenantService.csproj GS.TenantService/

RUN dotnet restore GS.TenantService/GS.TenantService.csproj

COPY GS.Core/ GS.Core/
COPY GS.MultiTenant/ GS.MultiTenant/
COPY GS.TenantService/ GS.TenantService/

RUN dotnet publish GS.TenantService/GS.TenantService.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
EXPOSE 5001

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GS.TenantService.dll"]
