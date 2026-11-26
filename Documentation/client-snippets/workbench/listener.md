```csharp
using Cratis.Chronicle.Workbench;
using Microsoft.AspNetCore.Builder;

public static class WorkbenchListenerConfiguration
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.UseCratisChronicleWorkbench(options =>
        {
            options.Address = "localhost";
            options.Port = 9876;
        });
    }
}
```
