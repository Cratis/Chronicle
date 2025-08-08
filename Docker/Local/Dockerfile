# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image.
####################################
FROM cratis/base-mongodb

WORKDIR /app

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/local-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

COPY /Source/Kernel/Server/out /app

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
