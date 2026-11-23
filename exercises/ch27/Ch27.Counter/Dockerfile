# Chapter 27: the counter, in two stages (as in Chapter 25). Build context: this folder.
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY Ch27.Counter.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ch27.Counter.dll"]
