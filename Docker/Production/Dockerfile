# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server - Preparation stage
####################################
# .NET 11 is in release candidate and MCR publishes no noble tags for it; Ubuntu 26.04 (resolute) is the distro, pinned to the RC.
# GA: switch to 11.0-resolute (and 11.0-resolute-chiseled-extra) before any image ships from this file.
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3-resolute AS prep
ARG VERSION

WORKDIR /app

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN ./copy-server-files.sh

RUN rm appsettings.Development.json && rm ./chronicle.json

####################################
# Cratis Chronicle Server - Runtime stage
####################################
# The -extra variant carries ICU and tzdata: the server runs with full globalization, which SQL Server storage needs.
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3-resolute-chiseled-extra

WORKDIR /app

EXPOSE 11111 30000 35000

# Copy prepared files from prep stage
COPY --from=prep /app/. ./

ENTRYPOINT ["./Cratis.Chronicle.Server"]
