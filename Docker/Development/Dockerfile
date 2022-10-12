# syntax=docker/dockerfile:1

####################################
# Cratis Server
# Build runtime image
####################################
FROM aksioinsurtech/cratis-base
ARG VERSION
ARG ENTRYPOINT=entrypoint.sh

RUN echo Version = ${VERSION}
RUN echo ${ENTRYPOINT}

WORKDIR /app

# Create entrypoint that runs both MongoDB and Runtime
COPY Docker/${ENTRYPOINT} ./entrypoint.sh
RUN chmod +x ./entrypoint.sh

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN echo $PWD
RUN ./copy-server-files.sh

EXPOSE 80 11111 30000
ENTRYPOINT ["/usr/bin/tini", "--", "/bin/bash", "/app/entrypoint.sh"]
