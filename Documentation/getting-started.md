# Getting started

## Pre requisites

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET Core 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node JS version 16](https://nodejs.org/)

## Install

The project comes with scaffolding templates.
Install it by simply doing the following from your terminal:

```shell
dotnet new -i Aksio.Templates
```

The templates are also supported through Visual Studio
by enabling [.NET CLI tooling](https://devblogs.microsoft.com/dotnet/net-cli-templates-in-visual-studio/).

## Usage

Create a folder to holds your microservice and from within this folder you run:

```shell
dotnet new <template-name>
```

| Template | Description |
| -------- | ----------- |
| aksioms  | Aksio Microservice template with ASP.NET Core for .NET 6 |

### AKSIOMS - Aksio Microservice

The Aksio Microservice template comes with an optional Web. By adding the `--IncludeWeb` option to
the command line, you'll get a setup with a React frontend with WebPack ready to go.

The result will be the following

```shell
<your microservice folder>
└───Main
└───Web
```

The template will do a post action to restore all **NuGet packages** as well as **node modules** if you
opted in for the web option.

#### Running

Before running the microservice backend and frontend, we will need to run the Cratis Kernel.
You can either use the `docker-compose.yml` file that comes with the template, or manually start it.

For the `docker compose` approach, make sure you're in the root folder of where the templated scaffolded
the setup and there is a `docker-compose.yml` file there. Then you can run:

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

If you prefer running it manually, simply do the following:

```shell
 docker run -p 27017:27017 -p 8080:80 -p 11111:11111 -p 30000:30000 aksioinsurtech/cratis:latest-development
```

> Note: The development image is a multi CPU architecture image supporting x64 and arm64 (e.g. Apple M1).
> For production, we only now have x64. Their tags are aksioinsurtech/cratis:latest or aksioinsurtech/cratis:<semver number>

Then you can simply run the microservice backend by running the following from the `Main` folder:

```shell
dotnet run
```

...or

```shell
dotnet watch run
```

> Note: if you're using an IDE such as Visual Studio or Rider, you typically would run it from within the IDE.

If you opted in to include a Web frontend, you run this by running the following from the `Web` folder:

```shell
yarn start:dev
```

The backend will start on port **5000** while the frontend will be on **9000**. The frontend is configured to
proxy `/api` and `/graphql` to the backend, meaning that you can do relative paths on the same origin for your
API calls from the frontend code.

#### Static Code Analysis

The template comes pre-configured with the Aksio static code analysis rules described [here](https://github.com/aksio-insurtech/Defaults).
You can find the rule-sets [here](https://github.com/aksio-insurtech/Defaults/tree/main/Source/Defaults).
For the Web part, it also comes with a pre-configured `.eslintrc.js` file with a set of rules and also a `tsconfig.json` file
with a default setup.
