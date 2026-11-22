# Chapter 25: Ch25.Notes as a container image, in two stages.
#   docker build -t ch25-notes .
# The base images are arguments, so you can swap them (a newer patch, or a mirror) without editing this file.
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

# Stage 1, "build": the full SDK (about 1 GB), used only to compile. None of it ends up in the final image.
FROM ${SDK_IMAGE} AS build
WORKDIR /src
# Copy the project file alone first and restore: this layer is cached, so a code change doesn't re-download packages.
COPY Ch25.Notes/Ch25.Notes.csproj Ch25.Notes/
RUN dotnet restore Ch25.Notes/Ch25.Notes.csproj
COPY Ch25.Notes/ Ch25.Notes/
RUN dotnet publish Ch25.Notes/Ch25.Notes.csproj -c Release -o /app --no-restore

# Stage 2, the image we ship: only the ASP.NET Core runtime, and our published files.
FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Not root: if someone breaks into the app, they get an ordinary user with no rights over the system.
USER $APP_UID
ENTRYPOINT ["dotnet", "Ch25.Notes.dll"]
