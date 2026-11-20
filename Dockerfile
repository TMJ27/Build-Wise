# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
COPY deployment/rds-certificates/ /usr/local/share/ca-certificates/
RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade perl-base libc6 libc-bin \
    && rm -rf /var/lib/apt/lists/* \
    && update-ca-certificates \
    && mkdir -p /app/keys /app/wwwroot/logos \
    && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
WORKDIR /app
ENV TZ=Africa/Harare
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BuildWise.Web.csproj", "."]
COPY ["BuildWise.Data/BuildWise.Data.csproj", "BuildWise.Data/"]
COPY ["BuildWise.Lib/BuildWise.Lib.csproj", "BuildWise.Lib/"]
RUN dotnet restore "./BuildWise.Web.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./BuildWise.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BuildWise.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
USER root
RUN rm -f /app/appsettings.Development.json
USER $APP_UID
ENTRYPOINT ["dotnet", "BuildWise.Web.dll"]
