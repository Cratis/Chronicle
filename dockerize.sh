#!/bin/bash

# Build .NET Server - Production (no development tools)
pushd .
cd ./Source/Kernel/Server
dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true --self-contained -o out/x64
dotnet publish -c Release -r linux-arm64 -p:PublishReadyToRun=true --self-contained -o out/arm64
popd

# Build .NET Server - Development (with development tools)
pushd .
cd ./Source/Kernel/Server
dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true -p:DevelopmentBuild=true --self-contained -o out-dev/x64
dotnet publish -c Release -r linux-arm64 -p:PublishReadyToRun=true -p:DevelopmentBuild=true --self-contained -o out-dev/arm64
popd

# Build Workbench - Production
pushd .
cd ./Source/Workbench
yarn build
popd

# Build Workbench - Development (with development tools UI)
pushd .
cd ./Source/Workbench
CHRONICLE_DEVELOPMENT=true yarn build --outDir ./wwwroot-dev
popd

# Build images
docker build -t cratis/chronicle -f ./Docker/Production/Dockerfile .
docker build -t cratis/chronicle:development -f ./Docker/Development/Dockerfile .

# For building multiple CPU architectures (PS: this automatically pushes to Docker Hub):
#docker buildx build -t cratis/chronicle:development -f ./Docker/Development/Dockerfile --platform linux/amd64,linux/arm64 --push .
