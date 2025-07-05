# Running Chronicle in local development

Chronicle is available as a [Docker Image](https://hub.docker.com/r/cratis/chronicle). For local development, we recommend using the development images. The `latest-development` tag will get you the most recent version.

The development image includes a MongoDB server, so you don't need any additional setup.

To run the server as a daemon, execute the following command in your terminal:

```shell
docker run -d -p 27017:27017 -p 8080:8080 -p 35000:35000 cratis/chronicle:latest-development
```

The command runs Chronicle as a **daemon** and exposes the following ports:

| Port  | Description |
| ----- | ----------- |
| 8080  | Chronicle workbench |
| 27017 | MongoDB port |
| 35000 | Chronicle client port |


## Docker compose

If you prefer to have a Docker Compose file, we recommend the following setup with Aspire to give
you open telemetry data:

{{snippet:Quickstart-DockerCompose}}
