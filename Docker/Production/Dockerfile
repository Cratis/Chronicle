# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server - Preparation stage
####################################
# MCR publishes no .NET 11 noble tags; use the GA images on Ubuntu 26.04 (resolute).
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute AS prep
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
# The kernel is published self-contained, so it carries its own .NET and ASP.NET Core runtime and only needs
# the native dependencies. The -extra variant carries ICU and tzdata: the server runs with full globalization,
# which SQL Server storage needs.
FROM mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute-chiseled-extra

WORKDIR /app

EXPOSE 11111 30000 35000

# Copy prepared files from prep stage
COPY --from=prep /app/. ./

ENTRYPOINT ["./Cratis.Chronicle.Server"]
