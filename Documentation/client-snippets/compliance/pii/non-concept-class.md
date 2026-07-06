```csharp
using Cratis.Chronicle.Compliance.GDPR;

// ❌ This will throw PIIAppliedToNonConceptAsType
[PII]
public class PiiAttrSomeArbitraryClass
{
}
```
