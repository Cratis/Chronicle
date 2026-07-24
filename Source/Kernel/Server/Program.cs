// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Api;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Server;
using Cratis.Chronicle.Server.Authentication;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Workbench;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;
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
var isInMemoryStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.InMemory, StringComparison.OrdinalIgnoreCase);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

if (chronicleOptions.Features.Api)
{
    builder.Services.AddCratisChronicleApi(useGrpc: false);
}

// The Chronicle port multiplexes gRPC (HTTP/2) and the Workbench, API and OAuth flows (HTTP/1.1)
// on a single port. Kestrel can only serve both protocols on one port over TLS, where ALPN
// negotiates the protocol per connection — cleartext HTTP/2 (h2c) cannot share a port with HTTP/1.1.
// A configured certificate is therefore required; in development one is generated automatically.
var certificate = CertificateLoader.LoadCertificate(chronicleOptions);
if (certificate is not null)
{
    logger.TlsCertificateLoaded();
}
else
{
#if DEVELOPMENT
    // The certificate must live for the lifetime of the process; Kestrel uses it for every TLS handshake.
#pragma warning disable CA2000 // Dispose objects before losing scope
    certificate = DevelopmentCertificate.Create();
#pragma warning restore CA2000
    logger.DevelopmentCertificateGenerated();
#else
    logger.TlsCertificateMissingProduction();
    throw new InvalidOperationException(
        "No TLS certificate is configured. The Chronicle port serves gRPC (HTTP/2) and the Workbench, " +
        "API and OAuth flows (HTTP/1.1) on a single TLS port, which requires a certificate. " +
        "Provide one through Tls:CertificatePath (and Tls:CertificatePassword) in configuration. " +
        "When TLS is terminated upstream by an ingress/reverse proxy, re-encrypt the connection to Chronicle.");
#endif
}

logger.ServerListening(chronicleOptions.Port);

builder.WebHost.UseKestrel(options =>
{
    // A single TLS port for both gRPC (HTTP/2) and the Workbench, API and OAuth flows (HTTP/1.1),
    // multiplexed per connection through ALPN.
    options.ListenAnyIP(chronicleOptions.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        listenOptions.UseHttps(certificate);
    });

    // Optionally expose the health endpoint on a dedicated port. The health endpoint is HTTP/1.1
    // only, so it can live on its own single-protocol port where TLS is optional — unlike the
    // main port, which must stay TLS to multiplex HTTP/1.1 and HTTP/2 through ALPN. Disabling TLS
    // here lets orchestrator and load-balancer probes that cannot validate a (self-signed)
    // certificate reach the endpoint in cleartext.
    if (chronicleOptions.DedicatedHealthPort is { } healthPort)
    {
        logger.HealthEndpointListening(healthPort, chronicleOptions.Health.Tls);
        options.ListenAnyIP(healthPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            if (chronicleOptions.Health.Tls)
            {
                listenOptions.UseHttps(certificate);
            }
        });
    }

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
       if (!isSqlStorage && !isInMemoryStorage)
       {
           mongo.Server = chronicleOptions.Storage.ConnectionDetails;
           mongo.Database = Cratis.Chronicle.Storage.MongoDB.WellKnownDatabaseNames.Chronicle;
       }
       else
       {
           // Placeholder values required to pass MongoDBOptions validation.
           // MongoDB services are removed from the DI container in SQL and in-memory modes and will not connect.
           mongo.Server = "mongodb://localhost:27017";
           mongo.Database = "chronicle_placeholder";
       }
   },
   builder => builder.WithCamelCaseNamingPolicy());

hostBuilder
   .UseOrleans(_ =>
   {
        var clustering = chronicleOptions.Clustering;
        if (clustering.Type == Cratis.Chronicle.Configuration.ClusteringType.MongoDB)
        {
            // Membership is kept in MongoDB (wired by WithMongoDB below) - nodes sharing the
            // same storage and cluster id form one cluster.
            _.Configure<Orleans.Configuration.ClusterOptions>(options =>
            {
                options.ClusterId = clustering.ClusterId;
                options.ServiceId = clustering.ServiceId;
            });

            if (clustering.AdvertisedIP is { } advertisedIP)
            {
                _.ConfigureEndpoints(System.Net.IPAddress.Parse(advertisedIP), clustering.SiloPort, clustering.GatewayPort);
            }
            else
            {
                _.ConfigureEndpoints(clustering.SiloPort, clustering.GatewayPort);
            }
        }
        else
        {
            if (!isSqlStorage && !isInMemoryStorage && ConnectionStringLocality.IsNonLocal(chronicleOptions.Storage.ConnectionDetails))
            {
                logger.LocalhostClusteringAgainstSharedStorage();
            }

            _.UseLocalhostClustering(clustering.SiloPort, clustering.GatewayPort, serviceId: clustering.ServiceId, clusterId: clustering.ClusterId);
        }

        _.AddChronicleToSilo(chronicleBuilder =>
        {
            if (isInMemoryStorage)
                chronicleBuilder.WithInMemory(chronicleOptions);
            else if (isSqlStorage)
                chronicleBuilder.WithSql(chronicleOptions);
            else
                chronicleBuilder.WithMongoDB(chronicleOptions);

            chronicleBuilder.WithVaultComplianceStorage(chronicleOptions);
            chronicleBuilder.WithAzureKeyVaultComplianceStorage(chronicleOptions);
        });
   })
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

       // Convention binding and authentication setup auto-register the storage implementations of every
       // referenced backend (MongoDB, SQL, and in-memory) alongside each other. Orleans resolves
       // IEnumerable<T> returning all, so the implementations of the backends that are NOT active must be
       // removed to prevent DI failures (e.g. MongoDB types require a MongoDB connection, SQL types require
       // ITableMigrator<>). This removal runs last to catch any backend types added by all extensions.
       //
       // Sink factories are exempt: every backend's ISinkFactory resolves its infrastructure dependency
       // lazily inside CreateFor rather than its constructor, so it is always safe to keep registered.
       // ChronicleOptions.DefaultSinkTypeId (a read-model sink choice) is independent of the Kernel's
       // storage backend - e.g. an app can run the Kernel on MongoDB while projecting some read models
       // to the in-memory sink - so removing a sink namespace here would break that combination.
       var activeBackendNamespace = "Cratis.Chronicle.Storage.MongoDB";
       if (isInMemoryStorage)
           activeBackendNamespace = "Cratis.Chronicle.Storage.InMemory";
       else if (isSqlStorage)
           activeBackendNamespace = "Cratis.Chronicle.Storage.Sql";

       string[] backendNamespaces =
       [
           "Cratis.Chronicle.Storage.InMemory",
           "Cratis.Chronicle.Storage.Sql",
           "Cratis.Chronicle.Storage.MongoDB"
       ];

       var inactiveStorageDescriptors = services
           .Where(sd =>
           {
               var ns = sd.ImplementationType?.Namespace;
               return ns is not null
                   && Array.Exists(backendNamespaces, ns.StartsWith)
                   && !ns.StartsWith(activeBackendNamespace)
                   && !ns.Contains(".Sinks", StringComparison.Ordinal);
           })
           .ToList();
       foreach (var descriptor in inactiveStorageDescriptors)
           services.Remove(descriptor);
   });

var app = builder.Build();

logger = app.Services.GetRequiredService<ILogger<Kernel>>();
logger.ServerConfigured();

// The kernel is never directly internet-facing - it always sits behind some reverse proxy (YARP in
// this repo's Composition, an ingress/load balancer in production). Without this, a proxied request
// that arrives over HTTPS at the proxy but HTTP between the proxy and the kernel (or vice versa)
// makes the kernel see the wrong scheme, so CookieSecurePolicy.SameAsRequest marks auth cookies
// Secure when the browser's own connection to the proxy was plain HTTP - a mismatch Chrome quietly
// tolerates for localhost but Safari correctly rejects, silently breaking sign-in. Clearing the known
// proxies/networks trusts the immediate proxy unconditionally, matching this always-proxied model.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseRouting();

app.UseCratisArc();

// The Workbench UI is built once and embedded into Cratis.Chronicle.Workbench - the kernel serves
// that same embedded asset set directly rather than expecting its own physical wwwroot. The
// embedded manifest only exists when the frontend was built before the Workbench assembly - a
// source build without the frontend output skips embedding entirely, and the kernel must keep
// running without the UI rather than fail at startup.
var workbenchAssembly = typeof(WorkbenchWebApplicationBuilderExtensions).Assembly;
var hasWorkbenchUI = workbenchAssembly.GetManifestResourceNames().Contains("Microsoft.Extensions.FileProviders.Embedded.Manifest.xml");
var serveWorkbench = chronicleOptions.Features.Workbench && chronicleOptions.Features.Api && hasWorkbenchUI;
if (chronicleOptions.Features.Workbench && !hasWorkbenchUI)
{
    logger.WorkbenchUINotEmbedded();
}

var workbenchStaticFileOptions = new StaticFileOptions();

// Map workbench static files BEFORE authentication so they are publicly accessible
if (serveWorkbench)
{
    var workbenchFileProvider = new ManifestEmbeddedFileProvider(
        workbenchAssembly,
        $"{typeof(WorkbenchWebApplicationBuilderExtensions).Namespace}.Files");
    workbenchStaticFileOptions = new StaticFileOptions { FileProvider = workbenchFileProvider };
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = workbenchFileProvider });
    app.UseStaticFiles(workbenchStaticFileOptions);
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

// Lets a client-side load balancer (e.g. LeastConnectionsLoadBalancerStrategy) ask this silo how
// busy it is before deciding whether to connect to it - anonymous so it can be probed before the
// client has authenticated, matching the health check endpoint above.
app.MapGet(
    "/connections/count",
    async (IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails) =>
        await grainFactory.GetConnectedClients(localSiloDetails.SiloAddress).GetConnectionCount())
    .AllowAnonymous();

// Reserves a connection slot ahead of the client actually connecting - see
// IConnectedClients.ReserveConnection for why. Anonymous for the same reason as the count above.
app.MapPost(
    "/connections/reserve",
    async (IGrainFactory grainFactory, ILocalSiloDetails localSiloDetails) =>
        await grainFactory.GetConnectedClients(localSiloDetails.SiloAddress).ReserveConnection())
    .AllowAnonymous();

// Kernel state reset is exposed via the gRPC IServer.ResetKernelState operation, which
// only honours the call in DEVELOPMENT builds. See Cratis.Chronicle.Services.Host.Server.

// Map workbench fallback route AFTER API endpoints to avoid conflicts
if (serveWorkbench)
{
    app.MapFallbackToFile("index.html", workbenchStaticFileOptions).AllowAnonymous();
}

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    logger.ServerShuttingDown();
    Console.WriteLine("******* SHUTTING DOWN CHRONICLE SERVER *******");
    cancellationToken.Cancel();
    eventArgs.Cancel = true;
};

logger.ServerStarted(chronicleOptions.Port);

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
