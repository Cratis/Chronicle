```csharp
using Cratis.Chronicle.EventSequences;

public static class ForwardEventPages
{
    public static async Task ReadHistory(IEventSequence sequence, CancellationToken cancellationToken)
    {
        var request = EventSequencePageRequest.Forward(null, 100);
        while (true)
        {
            var page = await sequence.GetPage(request, cancellationToken);
            foreach (var appended in page.Events)
            {
                Console.WriteLine(appended.Context.SequenceNumber);
            }

            if (!page.HasMore) break;
            request = request with { From = page.Next };
        }
    }
}
```
