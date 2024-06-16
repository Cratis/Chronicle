# Telemetry

The Cratis Kernel supports telemetry and has exporters for the following out of the box:

* Open Telemetry
* Azure Monitor (Application Insights)

To add telemetry to the Kernel, all you have to do is configure the `telemetry` option of the `chronicle.json`
file:

```json
{
    "telemetry": {
        "type": "<type>",
        "options": { }
    }
}
```

> Note: The options key holds a specific object that is for the type you configure.

## Open Telemetry

The configuration for Open Telemetry is as follows:

```json
{
    "telemetry": {
        "type": "open-telemetry",
        "options": {
            "endpoint": "<insert the url to export to>"
        }
    }
}
```

## Azure Monitor (Application Insights)

The configuration for Azure Monitor is as follows:

```json
{
    "telemetry": {
        "type": "app-insights",
        "options": {
            "key": "<insert the application insights instrumentation key>",
            "connectionString": "<insert the application insights connection string>"
        }
    }
}
```
