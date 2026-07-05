```csharp
using Cratis.Chronicle.Identities;

public class CorrelationIdentityCausationIdentity(IIdentityProvider identityProvider)
{
    public void SetForRequest(string subject, string name, string userName) =>
        identityProvider.SetCurrentIdentity(new Identity(subject, name, userName));

    public Identity GetCurrent() => identityProvider.GetCurrent();
}
```
