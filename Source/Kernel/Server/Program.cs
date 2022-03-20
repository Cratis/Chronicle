// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.MongoDB;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Schemas.MongoDB;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

#pragma warning disable SA1600
namespace Aksio.Cratis.Server;

public static class Program
{
    public static Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

        return CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
         Host.CreateDefaultBuilder(args)
            .UseAksio(MicroserviceId.Unspecified, _ => _.InSilo())
            .UseOrleans(_ => _
                .UseLocalhostClustering()
                .ConfigureServices(_ => _
                    .AddSingleton<IProjectionPositions, MongoDBProjectionPositions>()
                    .AddSingleton<IChangesetStorage, MongoDBChangesetStorage>()
                    .AddSingleton<IEncryptionKeyStore>(sp => new CacheEncryptionKeyStore(sp.GetService<MongoDBEncryptionKeyStore>()!))
                    .AddSingleton<ISchemaStore, MongoDBSchemaStore>()
                    .AddSingleton<IEventLogStorageProvider, MongoDBEventLogStorageProvider>()
                    .AddSingleton<IProjectionDefinitionsStorage, MongoDBProjectionDefinitionsStorage>()
                    .AddSingleton<IProjectionPipelineDefinitionsStorage, MongoDBProjectionPipelineDefinitionsStorage>()
                    .AddSingleton<IProjectionDefinitionsStorage, MongoDBProjectionDefinitionsStorage>())
                .Configure<EndpointOptions>(options =>
                {
                    options.SiloPort = 11111;
                    options.SiloListeningEndpoint = new IPEndPoint(IPAddress.Any, 11111);
                    options.GatewayPort = 30000;
                    options.GatewayListeningEndpoint = new IPEndPoint(IPAddress.Any, 30000);
                })
                .AddConnectedClientsTracking()
                .AddEventSequenceStream()
                .UseMongoDBReminderService()
                .AddSimpleMessageStreamProvider(ObservationConstants.ObserverHandlersStreamProvider, cs => cs.Configure(o => o.FireAndForgetDelivery = false))
                .AddExecutionContext())
            .ConfigureWebHostDefaults(_ => _
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
