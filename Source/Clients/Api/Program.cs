// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Api;
using Microsoft.AspNetCore.Server.Kestrel.Core;

AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

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
app
    .UseRouting()
    .UseCratisArc()
    .UseCratisChronicleApi();

Console.WriteLine($"Chronicle API started on port {chronicleApiOptions.ManagementPort}");

await app.RunAsync();

static void UnhandledExceptions(object sender, UnhandledExceptionEventArgs args)
{
    if (args.ExceptionObject is Exception exception)
    {
        Console.WriteLine("************ BEGIN UNHANDLED EXCEPTION ************");
        PrintExceptionInfo(exception);

        while (exception.InnerException != null)
        {
            Console.WriteLine("\n------------ BEGIN INNER EXCEPTION ------------");
            PrintExceptionInfo(exception.InnerException);
            exception = exception.InnerException;
            Console.WriteLine("------------ END INNER EXCEPTION ------------\n");
        }

        Console.WriteLine("************ END UNHANDLED EXCEPTION ************ ");
    }
}

static void PrintExceptionInfo(Exception exception)
{
    Console.WriteLine($"Exception type: {exception.GetType().FullName}");
    Console.WriteLine($"Exception message: {exception.Message}");
    Console.WriteLine($"Stack Trace: {exception.StackTrace}");
}
