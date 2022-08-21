FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
EXPOSE 80

# copy csproj and restore as distinct layers

COPY /src/PlexAniListSync.API/*.*.csproj ./src/PlexAniListSync.API/
COPY /src/PlexAniListSync.Models/*.*.csproj ./src/PlexAniListSync.Models/
COPY /src/PlexAniListSync.Services/*.*.csproj ./src/PlexAniListSync.Services/

RUN dotnet restore ./src/PlexAniListSync.API/

# copy everything else and build app
COPY ./src/ ./src

# Build and publish a release
RUN dotnet publish ./src/PlexAniListSync.API/ -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENV DOTNET_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "PlexAniListSync.API.dll"]
