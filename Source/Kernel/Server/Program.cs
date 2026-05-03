// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Api;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Server;
using Cratis.Chronicle.Server.Authentication;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql;
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

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
var logger = builder.Logging.Services
    .BuildServiceProvider()
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger<Kernel>();
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));
var assembly = Assembly.GetExecutingAssembly();
logger.ServerStarting(assembly.GetName().Version?.ToString() ?? "unknown");

var env = Environment.GetEnvironmentVariables();

ChronicleOptions.AddConfiguration(builder.Services, builder.Configuration);
var chronicleOptions = builder.Configuration.GetSection(ChronicleOptions.SectionPath).Get<ChronicleOptions>() ?? new ChronicleOptions();
var isSqlStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
    || string.Equals(chronicleOptions.Storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
    || string.Equals(chronicleOptions.Storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase);
builder.Services.AddHttpContextAccessor();
var healthChecks = builder.Services.AddHealthChecks();
if (!isSqlStorage)
{
    healthChecks.AddMongoDb(
        _ => new MongoDB.Driver.MongoClient(chronicleOptions.Storage.ConnectionDetails),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3));
}

if (chronicleOptions.Features.Api)
{
    builder.Services.AddCratisChronicleApi(useGrpc: false);
}

var grpcCertificate = CertificateLoader.LoadCertificate(chronicleOptions);
var workbenchCertificate = CertificateLoader.LoadWorkbenchCertificate(chronicleOptions);
var grpcTls = chronicleOptions.Tls;

if (!grpcTls.Enabled)
{
    logger.TlsDisabled();
}
else if (grpcCertificate is not null)
{
    logger.TlsCertificateLoaded();
}
else
{
#if DEVELOPMENT
    logger.TlsCertificateMissingDevelopment();
#else
    logger.TlsCertificateMissingProduction();
#endif
}

logger.ServerListening(chronicleOptions.ManagementPort, chronicleOptions.Port);

builder.WebHost.UseKestrel(options =>
{
    // Listen on ManagementPort for Workbench and API (HTTP/1.1)
    // Uses Workbench-specific TLS config, falling back to top-level TLS
    options.ListenAnyIP(chronicleOptions.ManagementPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;

        if (workbenchCertificate is not null)
        {
            listenOptions.UseHttps(workbenchCertificate);
        }
#if !DEVELOPMENT
        else
        {
            // In production, Workbench TLS can be explicitly disabled for deployments
            // behind an ingress/reverse proxy that terminates TLS upstream.
            var workbenchTls = chronicleOptions.WorkbenchTls;
            if (workbenchTls.Enabled)
            {
                throw new InvalidOperationException(
                    "No TLS certificate is configured for the Workbench. " +
                    "Either provide a certificate path in configuration, or set Workbench:Tls:Enabled to false " +
                    "if TLS is terminated upstream by an ingress/reverse proxy.");
            }
        }
#endif
    });

    // Listen on Port for gRPC (HTTP/2)
    // Uses top-level TLS config and can run without TLS when explicitly disabled.
    options.ListenAnyIP(chronicleOptions.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;

        if (grpcCertificate is not null)
        {
            listenOptions.UseHttps(grpcCertificate);
        }
#if !DEVELOPMENT
        else if (grpcTls.Enabled)
        {
            throw new InvalidOperationException(
                "No TLS certificate is configured for gRPC while TLS is enabled. " +
                "Please provide a certificate path in configuration, or set Tls:Enabled to false.");
        }
#endif
    });

    options.Limits.Http2.MaxStreamsPerConnection = 100;
});

var hostBuilder = builder.Host
.UseDefaultServiceProvider(_ =>
{
    _.ValidateScopes = false;
    _.ValidateOnBuild = false;
})
.AddCratisArc(options =>
{
    options.GeneratedApis.RoutePrefix = "api";
    options.GeneratedApis.SegmentsToSkipForRoute = 3;
})
.AddCratisMongoDB(
   configureOptions: mongo =>
   {
       if (!isSqlStorage)
       {
           mongo.Server = chronicleOptions.Storage.ConnectionDetails;
           mongo.Database = Cratis.Chronicle.Storage.MongoDB.WellKnownDatabaseNames.Chronicle;
       }
       else
       {
           // Placeholder values required to pass MongoDBOptions validation.
           // MongoDB services are removed from the DI container in SQL mode and will not connect.
           mongo.Server = "mongodb://localhost:27017";
           mongo.Database = "chronicle_placeholder";
       }
   },
   builder => builder.WithCamelCaseNamingPolicy());

hostBuilder
   .UseOrleans(_ => _
        .UseLocalhostClustering()
        .AddChronicleToSilo(chronicleBuilder =>
        {
            if (isSqlStorage)
                chronicleBuilder.WithSql(chronicleOptions);
            else
                chronicleBuilder.WithMongoDB(chronicleOptions);
        }))
   .ConfigureServices((context, services) =>
   {
       services.AddCodeFirstGrpcReflection();

       services
          .AddBindingsByConvention()
          .AddChronicleTelemetry(context.Configuration)
          .AddSelfBindings()
          .AddGrpcServices()
          .AddSingleton(BinderConfiguration.Default);

       // Add authentication services
       services.AddChronicleAuthentication(chronicleOptions);

       if (isSqlStorage)
       {
           // Convention binding and authentication setup auto-register MongoDB storage implementations
           // alongside SQL ones. Orleans resolves IEnumerable<T> returning all, so MongoDB implementations
           // must be removed to prevent DI failures (they require MongoDB infrastructure not available
           // in SQL mode). This removal runs last to catch any MongoDB types added by all extensions.
           var mongoStorageDescriptors = services
               .Where(sd => sd.ImplementationType?.Namespace?.StartsWith("Cratis.Chronicle.Storage.MongoDB") == true)
               .ToList();
           foreach (var descriptor in mongoStorageDescriptors)
               services.Remove(descriptor);
       }
   });

var app = builder.Build();

logger = app.Services.GetRequiredService<ILogger<Kernel>>();
logger.ServerConfigured();

app.UseRouting();

app.UseCratisArc();

// Map workbench static files BEFORE authentication so they are publicly accessible
if (chronicleOptions.Features.Workbench && chronicleOptions.Features.Api)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// Add authentication and authorization middleware AFTER routing but BEFORE endpoints
app.UseMiddleware<GrpcAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (chronicleOptions.Features.Api)
{
    // Configure API endpoints but without calling UseRouting again (already called above)
    app.UseWebSockets();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var resourceName = typeof(ApiApplicationBuilderExtensions).Namespace + ".SwaggerDark.css";
        using var stream = typeof(ApiApplicationBuilderExtensions).Assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var streamReader = new StreamReader(stream);
            var styles = streamReader.ReadToEnd();
            options.HeadContent = $"{options.HeadContent}<style>{styles}</style>";
        }
    });
}

// Map Identity API endpoints for SPA authentication - MUST be before MapControllers
app.MapGroup("/identity")
    .MapIdentityApi<User>()
    .AllowAnonymous();

// Map controllers for API and OAuth
if (chronicleOptions.Features.Api || chronicleOptions.Features.OAuthAuthority)
{
    app.MapControllers();
}

app.UseMiddleware<UserIdentityMiddleware>();
app.MapGrpcServices();
app.MapCodeFirstGrpcReflectionService();
app.MapHealthChecks(chronicleOptions.HealthCheckEndpoint).AllowAnonymous();

// Map workbench fallback route AFTER API endpoints to avoid conflicts
if (chronicleOptions.Features.Workbench && chronicleOptions.Features.Api)
{
    app.MapFallbackToFile("index.html").AllowAnonymous();
}

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.ServerShuttingDown();
    Console.WriteLine("******* SHUTTING DOWN CHRONICLE SERVER *******");
    cancellationToken.Cancel();
    eventArgs.Cancel = true;
};

logger.ServerStarted(
    chronicleOptions.ManagementPort,
    chronicleOptions.Port);

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
