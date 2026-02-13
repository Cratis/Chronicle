# Configuration Precedence

Configuration values are resolved in the following order, with later sources overriding earlier ones:

1. Default values
2. `chronicle.json` file
3. Environment variables

This allows you to set baseline configuration in `chronicle.json` and override specific values per environment.

## Example configuration

```json
{
  "port": 35000,
  "managementPort": 8080
}
```

| Source | Description |
| --- | --- |
| Defaults | Built-in values used when no configuration is provided |
| chronicle.json | File-based configuration loaded at startup |
| Environment variables | Overrides using `Cratis__Chronicle__` variables |

