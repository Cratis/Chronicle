# syntax=docker/dockerfile:1
# Build
FROM node:16 AS build
ARG VERSION=1.0.0
WORKDIR /build

COPY .editorconfig .
COPY .eslintrc.js .
COPY .prettierrc .
COPY .mocharc.js .
COPY package.json .
COPY tsconfig.json .
COPY ./Source/Node ./Source/Node
COPY ./Source/Workbench ./Source/Workbench

WORKDIR /build/Source/Workbench

# Set network timeout manually as a workaround till we're on Yarn 2: https://github.com/yarnpkg/yarn/issues/8242#issuecomment-661881292
RUN yarn config set network-timeout 300000
RUN yarn
RUN yarn build

# Server
FROM nginx

WORKDIR /app

COPY --from=build /build/Source/Workbench/wwwroot /usr/share/nginx/html
