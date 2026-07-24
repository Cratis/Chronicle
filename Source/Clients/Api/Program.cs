// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Api;
using Microsoft.AspNetCore.Server.Kestrel.Core;

ILogger<ChronicleApi>? logger = null;

// Route process-level unhandled exceptions through the logging pipeline so they reach the
// configured ILogger sinks and the OpenTelemetry exporter - not just the console. Until the
// logger is resolved (and if logging itself fails), fall back to writing to the console. (#1343)
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception exception)
    {
        LogCrash(log => log.UnhandledException(exception, args.IsTerminating), exception);
    }
};

TaskScheduler.UnobservedTaskException += (_, args) =>
{
    LogCrash(log => log.UnobservedTaskException(args.Exception), args.Exception);
    args.SetObserved();
};

// Force invariant culture for the Kernel
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("chronicle.json", optional: true, reloadOnChange: true);

var chronicleApiOptions = new ChronicleApiOptions();
builder.Configuration.Bind(chronicleApiOptions);
builder.Services.Configure<ChronicleApiOptions>(builder.Configuration);
builder.Services.AddCratisChronicleApi();

builder.Host
   .UseDefaultServiceProvider(_ =>
   {
       _.ValidateScopes = false;
       _.ValidateOnBuild = false;
   })
   .AddCratisArc(b => { });

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(chronicleApiOptions.ManagementPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
    options.Limits.Http2.MaxStreamsPerConnection = 100;
});

var app = builder.Build();
logger = app.Services.GetRequiredService<ILogger<ChronicleApi>>();
app
    .UseRouting()
    .UseCratisArc()
    .UseCratisChronicleApi();

Console.WriteLine($"Chronicle API started on port {chronicleApiOptions.ManagementPort}");

await app.RunAsync();

void LogCrash(Action<ILogger<ChronicleApi>> log, Exception exception)
{
    if (logger is not null)
    {
        try
        {
            log(logger);

            return;
        }
        catch (Exception loggingFailure)
        {
            // A failure while routing the crash through the logging pipeline must not mask the
            // original exception - fall back to the console output below.
            Console.WriteLine(loggingFailure);
        }
    }

    Console.WriteLine("************ UNHANDLED PROCESS-LEVEL EXCEPTION ************");
    Console.WriteLine(exception);
}
