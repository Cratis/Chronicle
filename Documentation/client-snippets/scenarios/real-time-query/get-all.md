```csharp
using Cratis.Chronicle;

public class ScenariosQueryOnLoanBooks(IEventStore eventStore)
{
    public async Task<IEnumerable<ScenariosQueryBook>> GetOnLoan()
    {
        var books = await eventStore.ReadModels.GetInstances<ScenariosQueryBook>();
        return books.Where(b => b.OnLoan);
    }
}
```
