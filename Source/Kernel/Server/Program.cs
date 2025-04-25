// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Api;
using Cratis.Chronicle.Concepts.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Server;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.DependencyInjection;
using Cratis.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

// Force invariant culture for the Kernel
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.Zero);
builder.Configuration.AddJsonFile("chronicle.json", optional: true, reloadOnChange: true);

var chronicleOptions = new ChronicleOptions();
builder.Configuration.Bind(chronicleOptions);
builder.Services.Configure<ChronicleOptions>(builder.Configuration);

if (chronicleOptions.Features.Api)
{
    builder.Services.AddCratisChronicleApi(useGrpc: false);
}

builder.WebHost.UseKestrel(options =>
{
    if (chronicleOptions.Features.Api)
    {
        options.ListenAnyIP(chronicleOptions.ApiPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
    }
    options.ListenAnyIP(chronicleOptions.Port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    options.Limits.Http2.MaxStreamsPerConnection = 100;
});

builder.Host
   .UseDefaultServiceProvider(_ =>
   {
       _.ValidateScopes = false;
       _.ValidateOnBuild = false;
   })
   .UseCratisApplicationModel()
   .UseCratisMongoDB(mongo =>
   {
       mongo.Server = chronicleOptions.Storage.ConnectionDetails;
       mongo.Database = WellKnownDatabaseNames.Chronicle;
   })
   .UseOrleans(_ => _
        .AddChronicleToSilo(_ => _
           .WithMongoDB(chronicleOptions))
        .UseDashboard(options =>
        {
            options.Host = "*";
            options.Port = 8081;
            options.HostSelf = true;
        }))
   .ConfigureServices((context, services) =>
   {
       services
          .AddSingleton(Globals.JsonSerializerOptions)
          .AddBindingsByConvention()
          .AddChronicleTelemetry(context.Configuration)
          .AddSelfBindings()
          .AddGrpcServices()
          .AddSingleton(BinderConfiguration.Default);

       services.AddCodeFirstGrpc();
   });

var app = builder.Build();
app.UseRouting();
app.UseCratisApplicationModel();

if (chronicleOptions.Features.Api)
{
    app.UseCratisChronicleApi();
}

if (chronicleOptions.Features.Workbench && chronicleOptions.Features.Api)
{
    app.UseDefaultFiles()
        .UseStaticFiles();

    app.MapFallbackToFile("index.html");
}
app.MapGrpcServices();

await app.RunAsync();

static void PrintExceptionInfo(Exception exception)
{
    Console.WriteLine($"Exception type: {exception.GetType().FullName}");
    Console.WriteLine($"Exception message: {exception.Message}");
    Console.WriteLine($"Stack Trace: {exception.StackTrace}");
}

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
