```csharp
using System.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public record TaggingUserAnalytics(int LoginCount, int CriticalLoginCount);

public class TaggingUserAnalyticsReducer : IReducerFor<TaggingUserAnalytics>
{
    public TaggingUserAnalytics LoggedIn(TaggingUserLoggedIn @event, TaggingUserAnalytics? current, EventContext context)
    {
        var analytics = current ?? new TaggingUserAnalytics(0, 0);

        // Access tags from the event context
        var isCritical = context.Tags.Any(tag => tag.Value == "critical");

        return analytics with
        {
            LoginCount = analytics.LoginCount + 1,
            CriticalLoginCount = analytics.CriticalLoginCount + (isCritical ? 1 : 0)
        };
    }
}
```
