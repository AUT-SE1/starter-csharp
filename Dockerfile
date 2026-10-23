FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY StarterService.csproj ./
RUN dotnet restore
COPY Program.cs Orders.cs appsettings.json ./
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "StarterService.dll", "--urls", "http://0.0.0.0:8080"]
