# Aksio Cratis

[![C# Build](https://github.com/aksio-insurtech/Cratis/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/aksio-insurtech/Cratis/actions/workflows/dotnet-build.yml)
[![TS Build](https://github.com/aksio-insurtech/Cratis/actions/workflows/node-build.yml/badge.svg)](https://github.com/aksio-insurtech/Cratis/actions/workflows/node-build.yml)
[![Nuget](https://img.shields.io/nuget/v/cratis)](http://nuget.org/packages/cratis)

## Introduction

Read the [documentation](./Documentation/index.md) for all the details.
For general guidance on the core values and principles we @ Aksio use, read more [here](https://github.com/aksio-system/Home/blob/main/README.md).

If you want to jump into building this repository and possibly contributing, please refer to [contributing](./Documentation/contributing.md).

## Running the samples

Make sure you have the following installed:

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET Core 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node JS version 16](https://nodejs.org/)

The sample consists of a backend and a frontend.
Navigate to the [Sample](./Samples/Bank) folder.

Before running the microservice backend and frontend, we will need to run the Cratis Kernel.

```shell
docker compose up -d
```

This will bring up the Cratis Kernel and expose the following ports:

| Port | Description |
| ---- | ----------- |
| 27017 | MongoDB - used for events and projection results |
| 8080 | Workbench and API for kernel |
| 11111 | Clustering port |
| 30000 | Client to Kernel connectivity |

Within here you'll see a folder called [Main](./Samples/Bank/Main), which represents the backend startup.
Navigate to this and start the backend by running:

```shell
dotnet run
```

The frontend is located in the [Web](./Samples/Bank/Web) folder. While the backend is running in another terminal,
navigate to that folder and start it by running:

```shell
yarn start:dev
```

Open a browser and navigate to [http://localhost:9100/](http://localhost:9100/) and you can start playing
around with the sample.

> Note: The Cratis workbench is available [http://localhost:8080/](http://localhost:8080/)
