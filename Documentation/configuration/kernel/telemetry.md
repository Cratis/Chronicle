# Telemetry

The Cratis Kernel is built on top of Orleans which offers telemetry which is helpful for monitoring.
Within the `cratis.json` file you can configure the telemetry by adding the following key:

```json
{
    "telemetry": {
        "type": "app-insights",
        "options": {
            "key": "<insert the application insights instrumentation key>"
        }
    }
}
```

> Note: 29th of August, Cratis only supports Azure Application Insights.
