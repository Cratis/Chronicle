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

# Microsoft.Data.SqlClient does not support Globalization Invariant Mode and throws
# NotSupportedException on connection open. Server.csproj sets InvariantGlobalization=true
# for minimal-image mode which bakes the switch into runtimeconfig.json; patch it here so
# the MSSQL integration tests work. MongoDB and Postgres are unaffected.
RUN sed -i 's/"System.Globalization.Invariant": true/"System.Globalization.Invariant": false/g' \
    Cratis.Chronicle.Server.runtimeconfig.json

EXPOSE 8080 11111 30000 35000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
