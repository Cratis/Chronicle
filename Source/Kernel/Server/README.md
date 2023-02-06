# Kernel

There is a `docker-compose.yml` file in the folder for Kernel server.
This sets up what is needed to work with Kernel development.

## Seq logging

[Seq](https://datalust.co/seq) has been configured as logging tool in the `docker-compose.yml` file.
To use it for the kernel the Serilog Sink is configured in the `appsettings.Development.json` file as below.
If you're running without Seq, remove the config.

```json
{
    "Serilog": {
    {
        "WriteTo": [
            {
                "Name": "Seq",
                "Args": {
                    "serverUrl": "http://localhost:5341"
                }
            }
        ]
    },
}
````
