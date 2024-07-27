using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public interface IMyAggregateRoot : IAggregateRoot
{
    Task DoStuff();
}
