
namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots;

public record StateProperty<TValue>(TValue Value, int CallCount)
{
    public static StateProperty<TValue> Empty => new(default, 0);

    public StateProperty<TValue> New(TValue value) => IncrementCallCount() with { Value = value };

    public StateProperty<TValue> IncrementCallCount() => this with { CallCount = CallCount + 1 };
}