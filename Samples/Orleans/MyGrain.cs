using Cratis.Chronicle;

namespace Orleans;

public class MyGrain(IChronicleClient client) : Grain, IMyGrain
{
    readonly IChronicleClient _client = client;

    public async Task DoStuff()
    {
        var eventStore = _client.GetEventStore("some_event_store");
        await eventStore.EventLog.Append("some_event", new MyFirstEvent());
    }
}
