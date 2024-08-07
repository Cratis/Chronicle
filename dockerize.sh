#!/bin/bash

# Build .NET Server
pushd .
cd ./Source/Kernel/Server
dotnet publish -c Release -r linux-x64 -p:PublishReadyToRun=true --self-contained -o out/x64
dotnet publish -c Release -r linux-arm64 -p:PublishReadyToRun=true --self-contained -o out/arm64
popd

# Build Workbench
pushd .
cd ./Source/Workbench
yarn build
popd

# Build images
docker build -t cratis/chronicle -f ./Docker/Production/Dockerfile .
docker build -t cratis/chronicle:development -f ./Docker/Development/Dockerfile .

# For building multiple CPU architectures (PS: this automatically pushes to Docker Hub):
#docker buildx build -t cratis/chronicle:development -f ./Docker/Development/Dockerfile --platform linux/amd64,linux/arm64 --push .
