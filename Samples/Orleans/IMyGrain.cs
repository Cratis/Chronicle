namespace Orleans;

public interface IMyGrain : IGrainWithGuidKey
{
    Task DoStuff();
}
