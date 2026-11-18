# ETAPA 1: Restaurar, compilar y publicar
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build

SHELL ["cmd", "/S", "/C"]

WORKDIR C:/src

COPY . .

RUN nuget restore ExcProy.sln -NonInteractive

RUN msbuild ExcProy\ExcProy.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:DeployDefaultTarget=WebPublish /p:WebPublishMethod=FileSystem /p:PublishUrl=C:\publish /p:DeleteExistingFiles=true

RUN dir C:\publish

# ETAPA 2: Preparar el entorno de ejecucion
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019

WORKDIR C:/inetpub/wwwroot

RUN powershell -NoProfile -Command "Remove-Item C:\inetpub\wwwroot\* -Recurse -Force"

COPY --from=build C:/publish/ ./

EXPOSE 80

ENTRYPOINT ["C:\\ServiceMonitor.exe", "w3svc"]