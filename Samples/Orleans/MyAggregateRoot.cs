using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public class MyAggregateRoot : AggregateRoot<MyAggregateRootState>, IMyAggregateRoot
{
    public async Task DoStuff()
    {
        await Apply(new MyFirstEvent("Blah"));
    }
}
