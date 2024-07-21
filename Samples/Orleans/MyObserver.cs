using Cratis.Chronicle.Reactions;

namespace Orleans;

public class MyObserver : IReaction
{
    public Task StuffHappened(MyFirstEvent @event)
    {
        Console.WriteLine("Stuff happened");
        return Task.CompletedTask;
    }
}
