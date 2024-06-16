# syntax=docker/dockerfile:1

####################################
# Chronicle Server
# Build runtime image
####################################
FROM mcr.microsoft.com/dotnet/runtime:7.0.8-bullseye-slim
ARG CONFIGURATION=Release
ARG VERSION

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

EXPOSE 80 11111 30000

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN echo $PWD
RUN ./copy-server-files.sh

RUN rm appsettings.Development.json
RUN rm ./chronicle.json

ENTRYPOINT ["./Cratis.Chronicle.Server"]
