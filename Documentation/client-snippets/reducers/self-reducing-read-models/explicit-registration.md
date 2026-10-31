```csharp
using Cratis.Chronicle;

// These excerpts reuse SelfAccountBalance from the basic example.
public static class SelfBalanceRegistrations
{
    public static void ConfigureGeneric(ChronicleOptions options) =>
        options.ExplicitArtifacts.RegisterReducer<SelfAccountBalance>("account-balances");

    public static void ConfigureRuntimeType(ChronicleOptions options) =>
        options.ExplicitArtifacts.RegisterReducer(typeof(SelfAccountBalance), "account-balances");
}
```
