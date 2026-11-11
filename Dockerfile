# Stage 1: build with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Restore first, from only the files restore needs. This layer is cached until a package changes.
# .editorconfig too: it configures the analyzers, and TreatWarningsAsErrors makes them part of the build.
COPY global.json nuget.config .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/Ticketing.Api/Ticketing.Api.csproj src/Ticketing.Api/packages.lock.json src/Ticketing.Api/
RUN dotnet restore src/Ticketing.Api/Ticketing.Api.csproj --locked-mode

# Then the source, then publish without restoring again.
COPY src/ src/
RUN dotnet publish src/Ticketing.Api/Ticketing.Api.csproj -c Release -o /app --no-restore

# Stage 2: run on the small, shell-less "chiseled" ASP.NET image. Nothing from the SDK ships.
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .

# Chiseled images already default to a non-root user; saying it out loud costs nothing.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ticketing.Api.dll"]
