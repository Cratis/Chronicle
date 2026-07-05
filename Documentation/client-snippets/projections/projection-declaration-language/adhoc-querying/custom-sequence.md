```csharp
var result = await projections.Query(
    """
    projection InboxMessages
      from MessageReceived
    """,
    eventSequenceId: "inbox");
```
