```csharp
public static class ComplianceErasureAllowNewKey
{
    public static async Task Allow(ChronicleClient chronicleClient)
    {
        var eventStore = await chronicleClient.GetEventStore("Sales");
        await eventStore.PII.AllowNewEncryptionKeyFor("person-42");
    }
}
```
