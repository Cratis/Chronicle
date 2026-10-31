```csharp
using Cratis.Chronicle.EventSequences;

public static class LatestEventPages
{
    public static async Task ReadLatest(IEventSequence sequence, CancellationToken cancellationToken)
    {
        var filter = new EventSequenceFilter(Tags: ["billing", "priority"]);
        var latest = await sequence.GetLatest(50, filter, cancellationToken);
        if (latest.HasMore)
        {
            var older = await sequence.GetPage(
                EventSequencePageRequest.Backward(latest.Next, 50, filter),
                cancellationToken);
            foreach (var appended in older.Events)
            {
                Console.WriteLine(appended.Context.SequenceNumber);
            }
        }
    }
}
```
