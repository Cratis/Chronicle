// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Autofac.Extensions.DependencyInjection;
using Cratis.Compliance;
using Cratis.Compliance.MongoDB;
using Cratis.Events.Projections;
using Cratis.Events.Projections.Changes;
using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.MongoDB;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

namespace Cratis.Server
{
    public static class Program
    {
        public static Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptions;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                .UseOrleans(_ => _
                    .UseLocalhostClustering()
                    .ConfigureServices(_ => _
                        .AddSingleton<IProjectionPositions, MongoDBProjectionPositions>()
                        .AddSingleton<IChangesetStorage, MongoDBChangesetStorage>()
                        .AddSingleton<IEncryptionKeyStore, MongoDBEncryptionKeyStore>()
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
                    .AddEventLogStream()
                    .UseMongoDBReminderService()
                    .AddSimpleMessageStreamProvider("observer-handlers", cs => cs.Configure(o => o.FireAndForgetDelivery = false))
                    .AddExecutionContext())
                .UseCratis(Startup.Types, _ => _.InSilo())
                .UseSerilog()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(_ => _
                    .UseStartup<Startup>());

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
    }
}
