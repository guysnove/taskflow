# TaskFlow's container image (Course 1, Chapter 29; first built and fixed in Course 2, Chapter 25).
# Build from the repository root:   docker build -f Deploy/Dockerfile -t taskflow .
# Run it, with its data in a named volume:
#   docker run -d -p 8080:8080 -v taskflow-data:/data --name taskflow taskflow
# The base images are build arguments: CI uses Microsoft's; the course recording used its own (see docs/containers.md).
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

# Stage 1: restore, build and publish with the full SDK
FROM ${SDK_IMAGE} AS build
WORKDIR /src
# Restore first, from the project files AND their lock files only: this layer is cached until a dependency changes.
# Course 2, Chapter 24: --locked-mode, exactly as in CI. Course 2, Chapter 12: four projects; each must be listed
# here (DockerfileTests checks that none is missing).
COPY global.json Directory.Build.props ./
COPY TaskFlow/TaskFlow.csproj TaskFlow/packages.lock.json TaskFlow/
COPY TaskFlow.Application/TaskFlow.Application.csproj TaskFlow.Application/packages.lock.json TaskFlow.Application/
COPY TaskFlow.Domain/TaskFlow.Domain.csproj TaskFlow.Domain/packages.lock.json TaskFlow.Domain/
COPY TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj TaskFlow.Infrastructure/packages.lock.json TaskFlow.Infrastructure/
RUN dotnet restore TaskFlow/TaskFlow.csproj --locked-mode
COPY TaskFlow/ TaskFlow/
COPY TaskFlow.Application/ TaskFlow.Application/
COPY TaskFlow.Domain/ TaskFlow.Domain/
COPY TaskFlow.Infrastructure/ TaskFlow.Infrastructure/
ARG VERSION=0.0.0-local
RUN dotnet publish TaskFlow/TaskFlow.csproj -c Release -o /app --no-restore -p:InformationalVersion=$VERSION

# Stage 2: run with the smaller ASP.NET Core runtime image
FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /app .
# Links the image to its repository on GitHub's container registry (and tells anyone where its source is).
LABEL org.opencontainers.image.source="https://github.com/guysnove/taskflow"
# The database and the sign-in keys live in /data: mount a named volume there, or they vanish with
# the container. Course 2, Chapter 25: the folder must exist AND belong to the non-root user, BEFORE "VOLUME"
# (a new volume starts as a copy of it). Without this line the first start failed: SQLite Error 14.
RUN mkdir -p /data && chown $APP_UID /data
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__TaskFlow="Data Source=/data/taskflow.db" \
    DataProtection__KeysPath=/data/keys
VOLUME /data
EXPOSE 8080
# Run as the image's non-root user ("app", UID 1654).
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskFlow.dll"]
