using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Orleans;

[EventType("b80f141b-54d2-4441-88dd-1364161c5bd6")]
public record MyFirstEvent();


public class MyGrain(IChronicleClient client) : Grain, IMyGrain
{
    readonly IChronicleClient _client = client;

    public async Task DoStuff()
    {
        var eventStore = _client.GetEventStore("some_event_store");
        await eventStore.EventLog.Append("some_event", new MyFirstEvent());
    }
}
