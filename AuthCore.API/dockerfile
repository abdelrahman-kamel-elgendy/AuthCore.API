# ── Stage 1: Build ────────────────────────────────────────────────────────────
# Uses the full SDK image to restore, build, and publish the app.
# This stage is discarded after the build — it never ends up in the final image.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies first.
# Docker caches this layer — restoring only re-runs when .csproj changes,
# not on every code change. This keeps builds fast.
COPY AuthCore.API.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish a release build
COPY . .
RUN dotnet publish AuthCore.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# Uses the much smaller ASP.NET runtime image (no SDK, no compiler).
# Final image size: ~200MB vs ~800MB for the SDK image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user to run the app — never run as root in production
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy only the published output from the build stage
COPY --from=build /app/publish .

# Copy email templates — these are read at runtime, not embedded in the binary
COPY Templates/ ./Templates/

# Give the non-root user ownership of the app directory
RUN chown -R appuser:appgroup /app
USER appuser

# Document the port the app listens on (actual binding is set via env var)
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080 (non-privileged port, good practice)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AuthCore.API.dll"]