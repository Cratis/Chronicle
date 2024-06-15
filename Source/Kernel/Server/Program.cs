// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Observation.Placement;
using Cratis.Chronicle.Server.Serialization;
using Cratis.DependencyInjection;
using Cratis.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

#pragma warning disable SA1600
namespace Cratis.Chronicle.Server;

public static class Program
{
    public static Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

        // Force invariant culture for the Kernel
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        return CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
         Host.CreateDefaultBuilder(args)
            .UseConfiguration()
            .UseDefaultServiceProvider(_ =>
            {
                _.ValidateScopes = false;
                _.ValidateOnBuild = false;
            })
            .UseLogging()
            .ConfigureCpuBoundWorkers()

            // TODO: Which extension method do we actually want to call here?
            .UseMongoDB()
            .ConfigureServices(services => services
                .AddSingleton(Globals.JsonSerializerOptions)
                .AddBindingsByConvention()
                .AddSelfBindings())
            .UseOrleans(_ => _
                .UseLocalhostClustering() // TODO: Implement MongoDB clustering
                .UseTelemetry() // TODO: Fix telemetry
                .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
                .AddBroadcastChannel(WellKnownBroadcastChannelNames.ProjectionChanged, _ => _.FireAndForgetDelivery = true)
                .ConfigureSerialization()
                .AddReplayStateManagement()
                .UseDashboard(options =>
                {
                    options.Host = "*";
                    options.Port = 8081;
                    options.HostSelf = true;
                })
                .ConfigureStorage()
                .UseMongoDB()
                .AddEventSequenceStreaming())
            .ConfigureWebHostDefaults(_ => _
                .ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(35000, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    options.Limits.Http2.MaxStreamsPerConnection = 100;
                })
                .UseStartup<Startup>());

    static void UnhandledExceptions(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            Log.Logger?.Error(exception, "Unhandled exception");
            Log.CloseAndFlush();
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
}
