```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

public class OrderDecisionController : ControllerBase
{
    public async Task<IActionResult> CommitDecision()
    {
        var completed = await HttpContext.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
        if (completed.GetDecisionConflicts().Any())
        {
            return Conflict(); // Re-read and retry the decision in a new request.
        }
        if (!completed.IsSuccess)
        {
            // Handle constraint violations and other append failures before writing the response.
            return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError);
        }
        return Ok();
    }
}
```
