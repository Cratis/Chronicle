---
description: Chronicle configuration precedence from built-in defaults through chronicle.json to Cratis__Chronicle__ environment variable overrides.
---
# Configuration Precedence

Chronicle combines built-in defaults, an optional `chronicle.json` file, and prefixed environment variables. Use the file for shared deployment settings and environment variables for values that differ between environments.

## Precedence Order

For the Chronicle-specific configuration sources, the order from **lowest to highest precedence** is:

1. **Built-in defaults** — property initializers on `ChronicleOptions` and its nested options supply values that are not otherwise configured.
2. **`chronicle.json`** — an optional file in the application base directory (next to the server executable, not the current working directory) overrides those defaults.
3. **Environment variables** — variables prefixed with `Cratis__Chronicle__` override matching file values.

The loader first combines the file and prefixed environment variables, then copies their non-null values into the host configuration under `Cratis:Chronicle`. This is the Chronicle loader's order, not a claim that every host configuration provider participates in that same sequence. A value absent from both Chronicle-specific sources can still be supplied by the host configuration.

## Example configuration

A deployment might keep this file alongside the server:

```json
{
    "events": {
        "queues": 2,
        "queueBoundedCapacity": 2000
    }
}
```

Override just the queue count before starting the server:

```bash
export Cratis__Chronicle__Events__Queues=4
```

The effective queue count is `4`; capacity remains `2000`. Other unspecified options retain their configured host values or built-in defaults. An environment override does not replace an entire object: each key is resolved separately.

## Key shapes

File settings are rooted directly at keys such as `events.queues`. Do **not** wrap the file in `Cratis` and `Chronicle` objects; the loader adds that path itself. Environment variables use double underscores for hierarchy, so `Cratis__Chronicle__Events__Queues` becomes `Cratis:Chronicle:Events:Queues` when published to host configuration.

If a change appears ineffective, check that `chronicle.json` sits in the application base directory next to the server executable, and check the environment first. A missing file is allowed, and a matching environment variable can mask a file edit. Invalid values are not a reason to expect a silent fallback to defaults; options binding and startup validation still apply.

## See also

- [Configuration file](configuration-file.md)
- [Environment variables](environment-variables.md)
- [Events](events.md)
