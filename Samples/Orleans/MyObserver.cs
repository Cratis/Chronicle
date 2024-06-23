using Cratis.Chronicle.Observation;

namespace Orleans;

[Observer("2f61d298-4176-44ba-b2d2-bbeae2deecb5")]
public class MyObserver
{
    public Task StuffHappened(MyFirstEvent @event)
    {
        Console.WriteLine("Stuff happened");
        return Task.CompletedTask;
    }
}
