# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image.
####################################
FROM cratis/base-mongodb

# Install .NET 10 runtime
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-runtime-10.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/local-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

COPY /Source/Kernel/Server/out /app

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
