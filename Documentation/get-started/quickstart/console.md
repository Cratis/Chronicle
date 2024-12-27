# Quickstart Console

## Pre-requisites

- [.NET 8 or higher](https://dot.net)
- [Docker Desktop or compatible](https://www.docker.com/products/docker-desktop/)

## Docker

Chronicle comes as a [Docker Image](https://hub.docker.com/r/cratis/chronicle).
For local development, we recommend using the development images. The tag `latest-development`
will get you the latest version of it.

The development image contains a MongoDB server and means that you don't need anything else.

You can run the server as a daemon by running the following command in your terminal:

```shell
docker run -d -p 27017:27017 -p 8080:8080 -p 35000:35000 cratis/chronicle:latest-development
```

If you prefer to have a Docker Compose file, we recommend the following setup with Aspire to give
you open telemetry data:

{{snippet:Quickstart-Console-DockerCompose}}

## Client

Chronicle is accessed through its client called `ChronicleClient`.
From this instance you can get the event store you want to work with.

The simplest thing you can do is to rely on the automatic discovery of artifacts by telling
the event store to discover and register everything automatically.

The following snippet configures the minimum and discovers everything for you.

{{snippet:Quickstart-Console-Setup}}

## Events

Defining an event is very simple. You can either use a C# `class` or a `record` type.
We recommend using a `record` type, since records are immutable, much like an [event](../../concepts/event.md)
should be.

With the type defined you simply add the `[EventType]` attribute for the new type.
The reason you do this is for the discovery system to be able to pick up all the event types
automatically.

Below defines a set of events we want to use for our library sample.

{{snippet:Quickstart-Console-Events}}
