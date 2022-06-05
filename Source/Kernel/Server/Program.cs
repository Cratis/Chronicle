// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.MongoDB;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Schemas.MongoDB;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Events.Store.MongoDB.Observation;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans;
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
            .UseAksio(_ => _.InKernel(), microserviceId: MicroserviceId.Kernel)
            .UseOrleans(_ => _
                .UseCluster()
                .UseDashboard(options =>
                {
                    options.Host = "*";
                    options.Port = 8081;
                    options.HostSelf = true;
                })
                .ConfigureServices(_ => _
                    .AddSingleton<IChangesetStorage, MongoDBChangesetStorage>()
                    .AddSingleton<IEncryptionKeyStore>(sp => new CacheEncryptionKeyStore(sp.GetService<MongoDBEncryptionKeyStore>()!))
                    .AddSingleton<ISchemaStore, MongoDBSchemaStore>()
                    .AddSingleton<IEventSequenceStorageProvider, MongoDBEventSequenceStorageProvider>()
                    .AddSingleton<IEventSequences, MongoDBEventSequences>()
                    .AddSingleton<IObserversState, MongoDBObserversState>()
                    .AddSingleton<IProjectionDefinitionsStorage, MongoDBProjectionDefinitionsStorage>()
                    .AddSingleton<IProjectionPipelineDefinitionsStorage, MongoDBProjectionPipelineDefinitionsStorage>()
                    .AddSingleton<IProjectionDefinitionsStorage, MongoDBProjectionDefinitionsStorage>())
                .AddConnectedClientsTracking()
                .AddEventSequenceStream()
                .UseMongoDBReminderService()
                .AddSimpleMessageStreamProvider("observer-handlers", cs => cs.Configure(o => o.FireAndForgetDelivery = false))
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
