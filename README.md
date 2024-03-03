# Cratis

## Packages / Deployables

[![Nuget](https://img.shields.io/nuget/v/Cratis?logo=nuget)](http://nuget.org/packages/cratis)
[![Docker](https://img.shields.io/docker/v/cratis/cratis?label=Cratis%20Kernel&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/cratis)

## Builds

[![C# Build](https://github.com/cratis/Cratis/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/Cratis/Cratis/actions/workflows/dotnet-build.yml)
[![Publish](https://github.com/cratis/Cratis/actions/workflows/publish.yml/badge.svg)](https://github.com/Cratis/Cratis/actions/workflows/publish.yml)
[![Documentation Site](https://github.com/cratis/Cratis/actions/workflows/pages.yml/badge.svg)](https://github.com/Cratis/Cratis/actions/workflows/pages.yml)

## Statistics

![Alt](https://repobeats.axiom.co/api/embed/51aa0aa357e84296b1d66b50d7143c917fee9471.svg "Repobeats analytics image")

## Introduction

Cratis is an Event Sourcing database built with ease of use, productivity, compliance and maintainability in mind.
It provides the core server, referred to as the **Kernel** with a client SDK to access it (.NET only for the time being) and tooling
built into it.

Read the documentation on our site [https://cratis.io](https://cratis.io) for all the details.
For general guidance on the core values and principles we @ use, read more [here](https://github.com/Cratis/.github/blob/main/profile/README.md).

If you want to jump into building this repository and possibly contributing, please refer to [contributing](./Documentation/contributing.md).

## Opening in VSCode online

If you prefer to browse the code in VSCode, you can do so by clicking [here](https://vscode.dev/github/cratis/Cratis).

## Running the samples

Make sure you have the following installed:

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET Core 7](https://dotnet.microsoft.com/download/dotnet/7.0)

The sample consists of a backend and a frontend.
Navigate to the [Bank Sample](./Samples/Banking/Bank) folder.

Before running the microservice backend and frontend, we will need to run the Cratis Kernel.

```shell
docker compose up -d
```

This will bring up the Cratis Kernel and expose the following ports:

| Port | Description |
| ---- | ----------- |
| 27017 | MongoDB - used for events and projection results |
| 8080 | Workbench and API for kernel |
| 8081 | Orleans Dashboard |
| 11111 | Clustering port |
| 30000 | Client to Kernel connectivity |

Within here you'll see a folder called [Main](./Samples/Banking/Bank/Main), which represents the backend startup.
Navigate to this and start the backend by running:

```shell
dotnet run
```

The frontend is located in the [Web](./Samples/Banking/Bank/Web) folder. While the backend is running in another terminal,
navigate to that folder and start it by running:

```shell
yarn start:dev
```

Open a browser and navigate to [http://localhost:9100/](http://localhost:9100/) and you can start playing
around with the sample.

> Note: The Cratis workbench is available [http://localhost:8080/](http://localhost:8080/)

## Contributing / Running locally

If you're looking to either contribute or dive into the code by building and running the Cratis Kernel locally,
you can read more [here](./Documentation/contributing.md). You'll find issues to start with by going to [here](https://github.com/cratis/Cratis/contribute).
