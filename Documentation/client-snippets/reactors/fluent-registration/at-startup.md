```csharp
using Microsoft.Extensions.Hosting;

public static class FluentReactorAtStartup
{
    public static void Configure(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddCratisChronicle(configureOptions: options => options.ExplicitArtifacts
            .RegisterReactor(
                "invoice-log",
                reactor => reactor
                    .On<InvoiceIssued>(@event => Console.WriteLine($"Issued to {@event.Customer}"))
                    .NotReplayable()));
    }
}
```
