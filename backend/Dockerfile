# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY XActBackend.slnx ./
COPY Directory.Build.props ./

COPY XActBackend/*.csproj ./XActBackend/
COPY XActBackend.Core/*.csproj ./XActBackend.Core/
COPY XActBackend.Persistence/*.csproj ./XActBackend.Persistence/
COPY XActBackend.Shared/*.csproj ./XActBackend.Shared/
COPY XActBackend.Importer/*.csproj ./XActBackend.Importer/
COPY XActBackend.Test/*.csproj ./XActBackend.Test/
COPY XActBackend.TestInt/*.csproj ./XActBackend.TestInt/

RUN dotnet restore

COPY . .
WORKDIR "/src/XActBackend"

RUN dotnet build "XActBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XActBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5200
ENV ASPNETCORE_URLS=http://+:5200

ENTRYPOINT ["dotnet", "XActBackend.dll"]