```csharp
using Cratis.Arc.Queries.ModelBound;

// Error CHR0035: Read model 'Customer' declares a property named '_subject', which
// Chronicle reserves as an internal MongoDB field. Rename it.
[ReadModel]
public record Customer(Guid Id, string Name, string _subject);
```
