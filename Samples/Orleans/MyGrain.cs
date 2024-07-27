using Cratis.Chronicle;

namespace Orleans;

public class MyGrain(IEventStore eventStore) : Grain, IMyGrain
{
    public async Task DoStuff()
    {
        await eventStore.EventLog.Append("some_event", new MyFirstEvent("Blah"));
    }
}
