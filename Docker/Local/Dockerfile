# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image.
####################################
FROM cratis/base-mongodb

WORKDIR /app

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/local-entrypoint.sh ./entrypoint.sh
# Sourced by the entrypoint to name whatever already holds the server's port when a bind fails - see #4048.
COPY Docker/report-port-holder.sh ./report-port-holder.sh
RUN chmod +x ./entrypoint.sh

COPY /Source/Kernel/Server/out /app

# Restore the executable bit on the apphost — publish artifacts can arrive without it
# (upload-artifact drops Unix permissions) and the entrypoint runs it directly (exit 126 otherwise).
RUN chmod +x ./Cratis.Chronicle.Server

EXPOSE 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
