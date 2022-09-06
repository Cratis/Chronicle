# syntax=docker/dockerfile:1

####################################
# Cratis Server
# Build runtime image
####################################
FROM mcr.microsoft.com/dotnet/runtime:6.0-focal
ARG CONFIGURATION=Release
ARG VERSION

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

EXPOSE 80 11111 30000

COPY ./Source/Kernel/Server/out/x64/*.dll .
COPY ./Source/Kernel/Server/out/x64/*.json .
COPY ./Source/Kernel/Server/out/x64/*.so .
COPY ./Source/Kernel/Server/out/x64/Aksio.Cratis.Server .
COPY ./Source/Workbench/wwwroot wwwroot

RUN rm ./cratis.json

ENTRYPOINT ["./Aksio.Cratis.Server"]
