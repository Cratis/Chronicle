# cratis

[![C# Build](https://github.com/aksio-insurtech/Cratis/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/aksio-insurtech/Cratis/actions/workflows/dotnet-build.yml)
[![TS Build](https://github.com/aksio-insurtech/Cratis/actions/workflows/node-build.yml/badge.svg)](https://github.com/aksio-insurtech/Cratis/actions/workflows/node-build.yml)
[![Nuget](https://img.shields.io/nuget/v/cratis)](http://nuget.org/packages/cratis)


## Build notes

- Orleans CodeGenerator - if it takes a long time on every build, perform a `dotnet clean` from the project you're working on and then `dotnet build` - subsequent builds should then be fast.
