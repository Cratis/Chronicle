# DSL

Automapping properties from event type(s)

```
{ReadModel} =
    {EventType}
```

Specifying keys:

```
{ReadModel} =
    {EventType}
        key = {eventSourceId}
        key =
            {property}={EventType}.{property}
            {secondProperty}={EventType}.{secondProperty}
```

Explicit mapping of properties from event types

```
{ReadModel} =
    {EventType}
        {property}={EventType}.{property}
        {property}+{EventType}.{property}
        {property}-{EventType}.{property}
        {property}=occurred
        {property}=eventSourceId
        {property}++
        {property}--
        {property}={value}
        {property}={EventType}.{property} join {property} on {EventType}.{property}
```

Defining event type that will remove

```
{ReadModel} =
    removedWith {EventType}
```

Defining children

```
{ReadModel} =
    {childrenProperty}=[
        identified by {property}
        # All operations are recursive within children
    ]
```
