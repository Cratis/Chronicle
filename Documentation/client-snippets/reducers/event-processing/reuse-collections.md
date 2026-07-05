```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

[EventType]
public record EventProcessingReuseItemAdded(Guid ItemId, string Name);

public record EventProcessingItem(Guid ItemId, string Name);
public record EventProcessingItemList(List<EventProcessingItem> Items);

public class EventProcessingItemListReducer : IReducerFor<EventProcessingItemList>
{
    public EventProcessingItemList ItemAdded(EventProcessingReuseItemAdded @event, EventProcessingItemList? current)
    {
        // Copy rather than mutate current.Items directly — a held snapshot may still reference it
        var items = new List<EventProcessingItem>(current?.Items ?? [])
        {
            new(@event.ItemId, @event.Name)
        };

        return new EventProcessingItemList(items);
    }
}
```
