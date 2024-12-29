# Running with Docker Compose

Using the development image of Chronicle is very convenient for getting started quickly.
Sometimes you want more control over the running environment and have MongoDB as its own
thing and possible other services, for collecting logging and other telemetry
both from Chronicle and your own app running on top.

The following configures a `docker-compose-yml` with Chronicle and [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

{{snippet:Quickstart-DockerCompose}}

With this setup you will have Chronicle running alongside [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview),
which will provide you with a dashboard for Open Telemetry.
