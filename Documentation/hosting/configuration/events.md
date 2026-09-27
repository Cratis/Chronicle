# Events

Event configuration controls how Chronicle Server queues appended events and retains causation properties.

## Example configuration

```json
{
  "events": {
    "queues": 8,
    "causationPropertyRetention": "Omit"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| queues | number | 2 | Number of appended event queues to use |
| causationPropertyRetention | `Retain` or `Omit` | `Retain` | Whether to persist causation property values on new appends, revisions and redactions. `Omit` keeps each causation entry's type and occurred time but stores no properties. |

