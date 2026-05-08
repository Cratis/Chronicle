# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server - Preparation stage
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS prep
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

WORKDIR /app

EXPOSE 8080 11111 30000 35000

# Copy prepared files from prep stage
COPY --from=prep /app/. ./

ENTRYPOINT ["./Cratis.Chronicle.Server"]
