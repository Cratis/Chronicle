# Events

Event configuration controls how Chronicle Server queues appended events.

## Example configuration

```json
{
  "events": {
    "queues": 8
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| queues | number | 8 | Number of appended event queues to use |

