# Telemetry

The Cratis Kernel is built on top of Orleans which offers telemetry which is helpful for monitoring.

## cratis.json

In the `cratis.json` file one can configure the telemetry by adding the following key:

```json
"telemetry": {
    "type": "app-insights",
    "options": {
        "key": "<insert the application insights instrumentation key>"
    }
}
```

> Note: 29th of August, Cratis only supports Azure Application Insights.
