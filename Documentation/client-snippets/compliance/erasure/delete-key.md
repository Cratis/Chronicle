```csharp
public static class ComplianceErasureDeleteKey
{
    public static async Task Delete(ChronicleClient chronicleClient)
    {
        var eventStore = await chronicleClient.GetEventStore("Sales");
        await eventStore.PII.DeleteEncryptionKeyFor("person-42");
    }
}
```
