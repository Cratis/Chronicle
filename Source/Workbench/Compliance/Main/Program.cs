// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac.Extensions.DependencyInjection;
using Serilog;

namespace Cratis.Workbench.Compliance.Main
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
                .UseCratis(Startup.Types)
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
