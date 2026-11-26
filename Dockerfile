FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/PropertyMaintenance.Api/PropertyMaintenance.Api.csproj src/PropertyMaintenance.Api/
RUN dotnet restore src/PropertyMaintenance.Api/PropertyMaintenance.Api.csproj
COPY src/PropertyMaintenance.Api/ src/PropertyMaintenance.Api/
COPY database/ database/
RUN dotnet publish src/PropertyMaintenance.Api/PropertyMaintenance.Api.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish/ .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "PropertyMaintenance.Api.dll"]
