using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public class MyAggregateRoot : AggregateRoot, IMyAggregateRoot
{
    public async Task DoStuff()
    {
        await Apply(new MyFirstEvent("Blah"));
    }

    public void On(MyFirstEvent @event)
    {
        Console.WriteLine($"Got an event with content '{@event}'");
    }
}
