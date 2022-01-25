# syntax=docker/dockerfile:1
# Server Build
ARG CONFIGURATION=Release
ARG VERSION
FROM mcr.microsoft.com/dotnet/sdk:6.0.101 AS server-build
WORKDIR /build

COPY .editorconfig .
COPY global.json .
COPY Directory.Build.props .
COPY Versions.props .
COPY ./Source ./Source

WORKDIR /build/Source/Kernel/Server

ADD Docker/build-server.sh .
RUN chmod +x ./build-server.sh
RUN ./build-server.sh

# Workbench
ARG VERSION
FROM aksioinsurtech/cratis:$VERSION-workbench as workbench

####################################
# Cratis Server
# Build runtime image
####################################
FROM mcr.microsoft.com/dotnet/runtime:6.0.1-focal
ARG CONFIGURATION=Release
ARG VERSION

WORKDIR /app

RUN echo Configuration = ${CONFIGURATION}
RUN echo Version = ${VERSION}

EXPOSE 80 11111 30000

COPY --from=server-build /build/Source/Kernel/Server/out/*.dll .
COPY --from=server-build /build/Source/Kernel/Server/out/*.json .
COPY --from=server-build /build/Source/Kernel/Server/out/*.so .
COPY --from=server-build /build/Source/Kernel/Server/out/Aksio.Cratis.Server .
COPY --from=workbench /usr/share/nginx/html wwwroot

ENTRYPOINT ["./Aksio.Cratis.Server"]
