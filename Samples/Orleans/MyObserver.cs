using Cratis.Chronicle.Reactions;

namespace Orleans;

[Reaction]
public class MyObserver
{
    public Task StuffHappened(MyFirstEvent @event)
    {
        Console.WriteLine("Stuff happened");
        return Task.CompletedTask;
    }
}
