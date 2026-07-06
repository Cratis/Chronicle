```csharp
using MongoDB.Driver;

public class GetStartedBooks(IMongoCollection<GetStartedBook> collection)
{
    public IEnumerable<GetStartedBook> OnLoan() => collection.Find(b => b.OnLoan).ToList();
}
```
