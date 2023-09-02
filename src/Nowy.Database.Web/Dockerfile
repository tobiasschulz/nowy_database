FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /repo
COPY . .
RUN rm -f global.json
WORKDIR "/repo/src"
RUN dotnet msbuild "Nowy.Database.Web/Nowy.Database.Web.csproj" /t:restore /p:Configuration=Release /p:OutputPath=/app/build
RUN dotnet msbuild "Nowy.Database.Web/Nowy.Database.Web.csproj" /t:build /p:Configuration=Release /p:OutputPath=/app/build
RUN dotnet msbuild "Nowy.Database.Web/Nowy.Database.Web.csproj" /t:build /p:Configuration=Release /p:OutputPath=/app/build

FROM build AS publish
COPY src/Nowy.Database.Web /app/publish
RUN dotnet publish -c Release -r linux-x64 -o /app/publish "Nowy.Database.Web/Nowy.Database.Web.csproj"
RUN ls -lh /app/publish/

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nowy.Database.Web.dll"]

