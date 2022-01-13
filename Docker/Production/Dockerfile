# syntax=docker/dockerfile:1
FROM scratch as shared
ARG CONFIGURATION=Release
ARG VERSION
WORKDIR /build

COPY .editorconfig .
COPY .eslintrc.js .
COPY .prettierrc .
COPY .mocharc.js .
COPY package.json .
COPY global.json .
COPY tsconfig.json .
COPY Directory.Build.props .
COPY Versions.props .
COPY ./Source ./Source

# Server Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS server-build
ARG CONFIGURATION=Release
ARG VERSION
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Kernel/Server

ADD Docker/build-server.sh .
RUN chmod +x ./build-server.sh
RUN ./build-server.sh

# Web Build
FROM node:16 AS web-build
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Workbench/Events/Web

# Set network timeout manually as a workaround till we're on Yarn 2: https://github.com/yarnpkg/yarn/issues/8242#issuecomment-661881292
RUN yarn config set network-timeout 300000
RUN yarn
RUN yarn build

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
COPY --from=server-build /build/Source/Kernel/Server/out/Cratis.Server .
COPY --from=web-build /build/Source/Workbench/Events/Web/wwwroot wwwroot

ENTRYPOINT ["./Cratis.Server"]
