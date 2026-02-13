# Storage

Configure the storage backend for Chronicle Server.

## Example configuration

```json
{
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://localhost:27017"
  }
}
```

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| type | string | Yes | Storage type (currently "MongoDB") |
| connectionDetails | string | Yes | MongoDB connection string |

