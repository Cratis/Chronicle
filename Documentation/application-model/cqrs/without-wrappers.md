# Without wrappers

When using either Commands or [Queries](./queries.md), the result is wrapped in a more descriptive
structure that will include the result returned by your controller action.This behavior is not an
opt-in on a per controller or action level, making it a cross cutting always on behavior.

If you need controller based endpoints that do not have the encapsulation of the `CommandResult` or the
`QueryResult` structures, you can use an attribute called `[AspNetResult]`.

The attribute can be used on a controller or a specific action. Once used, the result is kept as is from
the action called.

> Important note: The behavior of not allowing an invalid state is still kept intact, meaning that if you
> have validation that kicks in and makes the `ModelState`invalid, both the command and the query action
> filters will not call the action.

To keep the original result on a controller level, place the attribute before the controller:

```csharp
[Route("/api/accounts/debit")]
[AspNetResult]  // <-
public class Accounts : Controller
{
    [HttpGet("starting-with")]
    public async Task<IEnumerable<DebitAccount>> StartingWith([FromQuery] string? filter)
    {
        /* Code that gets the data and returns it */
    }

    [HttpGet("latest-transactions/{accountId}")]
    public DebitAccountLatestTransactions LatestTransactions([FromRoute] AccountId accountId)
    {
        /* Code that gets the data and returns it */
    }
}
```

To keep the original result on an action level, place the attribute before the action:

```csharp
[Route("/api/accounts/debit")]
public class Accounts : Controller
{
    [HttpGet("starting-with")]
    [AspNetResult]  // <-
    public async Task<IEnumerable<DebitAccount>> StartingWith([FromQuery] string? filter)
    {
        /* Code that gets the data and returns it */
    }

    [HttpGet("latest-transactions/{accountId}")]
    public DebitAccountLatestTransactions LatestTransactions([FromRoute] AccountId accountId)
    {
        /* Code that gets the data and returns it */
    }
}
```
