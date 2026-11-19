FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Exclusivo.sln ./
COPY src/Exclusivo.Web/Exclusivo.Web.csproj src/Exclusivo.Web/
COPY tests/Exclusivo.Tests/Exclusivo.Tests.csproj tests/Exclusivo.Tests/
RUN dotnet restore Exclusivo.sln

COPY . .
RUN dotnet publish src/Exclusivo.Web/Exclusivo.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Exclusivo.Web.dll"]
