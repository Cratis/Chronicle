```csharp
public static class ContributingKernelDateTimeOffsetUsage
{
    public static void Run()
    {
        var user = new ContributingKernelUser
        {
            Username = "john",
            CreatedAt = DateTimeOffset.UtcNow  // Implicit conversion from DateTimeOffset
        };

        DateTimeOffset created = user.CreatedAt;  // Implicit conversion to DateTimeOffset
        Console.WriteLine(created);
    }
}
```
