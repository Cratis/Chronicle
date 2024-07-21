using Cratis.Chronicle.Observation;

namespace Orleans;

[Observer]
public class MyObserver
{
    public Task StuffHappened(MyFirstEvent @event)
    {
        Console.WriteLine("Stuff happened");
        return Task.CompletedTask;
    }
}
