# syntax=docker/dockerfile:1
FROM scratch as shared
WORKDIR /build

COPY .editorconfig .
COPY .eslintrc.js .
COPY .prettierrc .
COPY .mocharc.js .
COPY package.json .
COPY global.json .
COPY tsconfig.json .
COPY Directory.Build.props .
COPY ./Source ./Source

# Server Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS server-build
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Kernel/Server

RUN dotnet publish -c Release -o out

# Web Build
FROM node:16 AS web-build
WORKDIR /build

COPY --from=shared /build/ .

WORKDIR /build/Source/Workbench/Events/Web

# Set network timeout manually as a workaround till we're on Yarn 2: https://github.com/yarnpkg/yarn/issues/8242#issuecomment-661881292
RUN yarn config set network-timeout 300000
RUN yarn
RUN yarn build

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Cratis Server
WORKDIR /app

EXPOSE 80 11111 30000
COPY --from=server-build /build/Source/Kernel/Server/out .
COPY --from=web-build /build/Source/Workbench/Events/Web/wwwroot wwwroot
ENTRYPOINT ["dotnet", "Cratis.Server.dll"]
