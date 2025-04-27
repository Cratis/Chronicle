# syntax=docker/dockerfile:1

####################################
# Cratis Chronicle Server
# Build runtime image
####################################
FROM cratis/base
ARG CONFIGURATION=Release
ARG VERSION

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

EXPOSE 8080 11111 30000 35000

COPY Docker/copy-server-files.sh ./copy-server-files.sh
RUN chmod +x ./copy-server-files.sh

COPY ./Source/Kernel/Server/out ./out
COPY ./Source/Workbench/Web/wwwroot wwwroot

RUN echo $PWD
RUN ./copy-server-files.sh

RUN rm appsettings.Development.json
RUN rm ./chronicle.json

ENTRYPOINT ["dotnet", "./Cratis.Chronicle.Server.dll"]

