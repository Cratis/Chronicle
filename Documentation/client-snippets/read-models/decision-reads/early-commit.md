```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
using Microsoft.AspNetCore.Http;

var completed = await HttpContext.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
if (completed.GetDecisionConflicts().Any())
{
    return Conflict(); // Re-read and retry the decision in a new request.
}
if (!completed.IsSuccess)
{
    // Handle constraint violations and other append failures before writing the response.
}
```
