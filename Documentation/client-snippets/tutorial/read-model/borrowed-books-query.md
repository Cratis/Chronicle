```csharp
using MongoDB.Driver;

public class BorrowedBooks(IMongoCollection<BorrowedBook> collection)
{
    public IEnumerable<BorrowedBook> All() => collection.Find(_ => true).ToList();
}
```
