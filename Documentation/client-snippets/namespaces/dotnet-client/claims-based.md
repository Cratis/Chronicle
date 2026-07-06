```csharp
using Cratis.Chronicle;

public static class NamespacesDotNetClientClaimsBased
{
    public static ChronicleClient CreateWithDefaultClaimType(ChronicleOptions options) =>
        // Uses the default claim type "tenant_id"
        new(options, namespaceResolver: new ClaimsBasedNamespaceResolver("tenant_id"));

    public static ChronicleClient CreateWithCustomClaimType(ChronicleOptions options) =>
        // Uses a custom claim type
        new(options, namespaceResolver: new ClaimsBasedNamespaceResolver("custom_tenant_claim"));
}
```
