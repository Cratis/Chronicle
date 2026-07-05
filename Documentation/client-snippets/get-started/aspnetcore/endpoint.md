```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

public static class AspNetCoreBookEndpoint
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/api/books/{bookId}/borrow", async (
            [FromServices] IEventLog eventLog,
            [FromRoute] Guid bookId,
            [FromQuery] string memberName) =>
                await eventLog.Append(bookId, new GetStartedBookBorrowed(memberName)));
    }
}
```
