using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Orleans;

public class MyAggregateRootReducer : IReducerFor<MyAggregateRootState>
{
    public Task<MyAggregateRootState> MyFirstEvent(MyFirstEvent @event, MyAggregateRootState? initial, EventContext context)
    {
        return Task.FromResult(new MyAggregateRootState());
    }
}
