# Kernel

There is a `docker-compose.yml` file in the folder for Kernel server.
This sets up what is needed to work with Kernel development.

## Seq logging

[Seq](https://datalust.co/seq) has been configured as logging tool in the `docker-compose.yml` file.
To use it for the kernel, add a Serilog Sink in the `appsettings.Development.json` file:

```json
{
    "Name": "Seq",
    "Args": {
        "serverUrl": "http://localhost:5341"
    }
}
````
