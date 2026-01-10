// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Api;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Server;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.DependencyInjection;
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
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));
ChronicleOptions.AddConfiguration(builder.Services, builder.Configuration);

var chronicleOptions = builder.Configuration.GetSection(ChronicleOptions.SectionPath).Get<ChronicleOptions>() ?? new ChronicleOptions();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks()
    .AddMongoDb(
        _ => new MongoDB.Driver.MongoClient(chronicleOptions.Storage.ConnectionDetails),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3));

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
   .AddCratisArc()
   .AddCratisMongoDB(
       configureOptions: mongo =>
       {
           mongo.Server = chronicleOptions.Storage.ConnectionDetails;
           mongo.Database = WellKnownDatabaseNames.Chronicle;
       },
       builder => builder.WithCamelCaseNamingPolicy())
   .UseOrleans(_ => _
        .UseLocalhostClustering()
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
       services.AddCodeFirstGrpc();
       services.AddCodeFirstGrpcReflection();

       services
          .AddBindingsByConvention()
          .AddChronicleTelemetry(context.Configuration)
          .AddSelfBindings()
          .AddGrpcServices()
          .AddSingleton(BinderConfiguration.Default);
   });

var app = builder.Build();
app.UseRouting();
app.UseCratisArc();

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
app.MapCodeFirstGrpcReflectionService();
app.MapHealthChecks(chronicleOptions.HealthCheckEndpoint);

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("******* SHUTTING DOWN CHRONICLE SERVER *******");
    cancellationToken.Cancel();
    eventArgs.Cancel = true;
};

await app.RunAsync(cancellationToken.Token);

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
