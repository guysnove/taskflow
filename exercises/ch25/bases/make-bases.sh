#!/usr/bin/env bash
# Chapter 25: this recording's machine can't reach any container registry (Docker Hub, mcr.microsoft.com, ghcr.io are blocked),
# but it can reach Ubuntu's package archive. So we build the two base images ourselves, the way the official ones are
# built: an Ubuntu 24.04 file system (debootstrap), plus Ubuntu's own .NET 10 packages, imported with "docker import".
set -euo pipefail
cd "$(dirname "$0")"
[ -d rootfs ] || debootstrap --variant=minbase noble rootfs http://archive.ubuntu.com/ubuntu/   # a minimal Ubuntu 24.04
echo "deb http://archive.ubuntu.com/ubuntu noble main universe
deb http://archive.ubuntu.com/ubuntu noble-updates main universe
deb http://archive.ubuntu.com/ubuntu noble-security main universe" > rootfs/etc/apt/sources.list
make() {   # make <folder> <packages…>
  local d=$1; shift; rm -rf "$d"; cp -a rootfs "$d"
  cp /etc/resolv.conf "$d/etc/resolv.conf"
  chroot "$d" bash -c "apt-get update -q && DEBIAN_FRONTEND=noninteractive apt-get install -y -q --no-install-recommends $* && apt-get clean && rm -rf /var/lib/apt/lists/*" > "$d.log" 2>&1
  : > "$d/etc/resolv.conf"
}
make rt-aspnet ca-certificates aspnetcore-runtime-10.0
make rt-sdk ca-certificates dotnet-sdk-10.0
make rt-postgres postgresql-16
make rt-nginx nginx                             # Chapter 27: a load balancer
make rt-redis redis-server                      # Chapter 27: a shared cache
# Like Microsoft's images: a non-root user "app" (UID 1654) that apps can switch to with USER $APP_UID.
for d in rt-aspnet rt-sdk; do chroot "$d" bash -c "groupadd -g 1654 app && useradd -u 1654 -g 1654 -m app"; done
# Like the official postgres image: an entrypoint that creates the database on first start, from environment variables.
cp postgres-entrypoint.sh rt-postgres/usr/local/bin/docker-entrypoint.sh && chmod 755 rt-postgres/usr/local/bin/docker-entrypoint.sh
# Inside the SDK image, NuGet must reach nuget.org through this machine's proxy, which re-signs HTTPS: trust its CA there.
cp /root/.ccr/ca-bundle.crt rt-sdk/usr/local/share/ca-certificates/recording-proxy.crt && chroot rt-sdk update-ca-certificates > /dev/null
tar -C rt-aspnet -c . | docker import --change 'ENV DOTNET_RUNNING_IN_CONTAINER=true' --change 'ENV APP_UID=1654' \
  --change 'ENV ASPNETCORE_HTTP_PORTS=8080' - local/dotnet-aspnet:10.0
tar -C rt-postgres -c . | docker import --change 'ENV PATH=/usr/lib/postgresql/16/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin' \
  --change 'ENV PGDATA=/var/lib/postgresql/data' --change 'EXPOSE 5432' --change 'ENTRYPOINT ["docker-entrypoint.sh"]' - local/postgres:16
tar -C rt-sdk -c . | docker import --change 'ENV DOTNET_RUNNING_IN_CONTAINER=true' --change 'ENV DOTNET_NOLOGO=true' --change 'ENV APP_UID=1654' - local/dotnet-sdk:10.0
tar -C rt-nginx -c . | docker import --change 'ENTRYPOINT ["nginx", "-g", "daemon off;"]' --change 'EXPOSE 80' - local/nginx:1
tar -C rt-redis -c . | docker import --change 'ENTRYPOINT ["redis-server", "--protected-mode", "no", "--save", ""]' --change 'EXPOSE 6379' - local/redis:7
docker images local/*
