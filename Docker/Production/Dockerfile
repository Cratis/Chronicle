# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG CONFIGURATION=Release
ARG VERSION

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

EXPOSE 8080 11111 30000 35000

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/wwwroot wwwroot

RUN echo $PWD
RUN ./copy-server-files.sh

RUN rm appsettings.Development.json
RUN rm ./chronicle.json

ENTRYPOINT ["./Cratis.Chronicle.Server"]
