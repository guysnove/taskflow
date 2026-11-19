# Chapter 29: a container image for TaskFlow. Build from the ch29/code folder:
#   docker build -f Deploy/Dockerfile -t taskflow .
# (Not built in the video: the recording environment couldn't download Microsoft's base images.)

# Stage 1: build and publish with the full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Course 2, Chapter 12: TaskFlow is four projects; restore needs every project file first.
COPY Directory.Build.props ./
COPY TaskFlow/TaskFlow.csproj TaskFlow/
COPY TaskFlow.Application/TaskFlow.Application.csproj TaskFlow.Application/
COPY TaskFlow.Domain/TaskFlow.Domain.csproj TaskFlow.Domain/
COPY TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj TaskFlow.Infrastructure/
RUN dotnet restore TaskFlow/TaskFlow.csproj
COPY TaskFlow/ TaskFlow/
COPY TaskFlow.Application/ TaskFlow.Application/
COPY TaskFlow.Domain/ TaskFlow.Domain/
COPY TaskFlow.Infrastructure/ TaskFlow.Infrastructure/
RUN dotnet publish TaskFlow/TaskFlow.csproj -c Release -o /app --no-restore

# Stage 2: run with the smaller ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The database and the data protection keys live in /data: mount a volume there, or they vanish with the container.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__TaskFlow="Data Source=/data/taskflow.db" \
    DataProtection__KeysPath=/data/keys
VOLUME /data
EXPOSE 8080
# Run as the image's non-root user; the /data volume must be writable by it.
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskFlow.dll"]
