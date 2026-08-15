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
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

ILogger<Kernel>? logger = null;

// Route process-level unhandled exceptions through the logging pipeline so they reach the
// configured ILogger sinks and the OpenTelemetry exporter - not just the console. Until the
// logger is resolved (and if logging itself fails), fall back to writing to the console. (#1343)
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    if (args.ExceptionObject is Exception exception)
    {
        LogCrash(log => log.UnhandledException(exception, args.IsTerminating), exception);
    }
};

TaskScheduler.UnobservedTaskException += (_, args) =>
{
    LogCrash(log => log.UnobservedTaskException(args.Exception), args.Exception);
    args.SetObserved();
};

// Force invariant culture for the Kernel
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
logger = builder.Logging.Services
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

            // Self-heal the membership table: Orleans ships with the defunct-silo sweep disabled,
            // so dead entries from restarts and failed rollouts accumulate until new nodes cannot join.
            _.Configure<Orleans.Configuration.ClusterMembershipOptions>(options =>
            {
                options.DefunctSiloCleanupPeriod = clustering.DefunctSiloCleanupPeriod > TimeSpan.Zero
                    ? clustering.DefunctSiloCleanupPeriod
                    : null;
                options.DefunctSiloExpiration = clustering.DefunctSiloExpiration;
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

// State the certificate ring at every boot. A rotation is carried out by restarting nodes with a changed
// ring, so this is the record of what each node actually loaded - the thing that has to match across the
// cluster, and the thing a restore has to reproduce. (#3690)
var encryptionCertificateRing = app.Services.GetRequiredService<Cratis.Chronicle.Security.IEncryptionCertificateRing>().GetStatus();
if (encryptionCertificateRing.IsConfigured)
{
    logger.EncryptionCertificateRingLoaded(encryptionCertificateRing.Certificates.Count(), encryptionCertificateRing.ActiveKeyId);
    foreach (var entry in encryptionCertificateRing.Certificates)
    {
        logger.EncryptionCertificateInRing(entry.KeyId, entry.Role, entry.Subject, entry.NotAfter, entry.CertificatePath);
    }

    foreach (var expired in encryptionCertificateRing.Certificates.Where(_ => _.HasExpired))
    {
        logger.EncryptionCertificateInRingHasExpired(expired.KeyId, expired.NotAfter);
    }
}
else
{
    logger.EncryptionCertificateRingNotConfigured();
}

// Opt-in: when the dedicated health port is exclusive, nothing but the health endpoint is answered
// on it. This is registered first so no later middleware or endpoint - Workbench static files, the
// REST API, the OAuth flows, the fallback - ever observes such a request. The decision keys on
// HttpContext.Connection.LocalPort, the port the socket actually accepted the connection on, which
// no client can influence; Host and the X-Forwarded-* headers are all client-supplied and therefore
// spoofable. HealthOnlyPortPolicy owns the decision so it can be specified in isolation. (#3604)
app.Use(async (context, next) =>
{
    if (HealthOnlyPortPolicy.ShouldReject(chronicleOptions, context.Connection.LocalPort, context.Request.Path.Value))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);
});

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

// The Workbench UI is built once and reaches a deployment either embedded into
// Cratis.Chronicle.Workbench or as files in the web root next to the binary - see WorkbenchUI for
// why both exist. Serve whichever is present, and keep running without the UI when neither is:
// a Kernel-only deployment legitimately ships no Workbench.
var workbenchAssembly = typeof(WorkbenchWebApplicationBuilderExtensions).Assembly;
var workbenchFileProvider = WorkbenchUI.Resolve(
    WorkbenchUI.ResolveEmbedded(workbenchAssembly, $"{typeof(WorkbenchWebApplicationBuilderExtensions).Namespace}.Files"),
    app.Environment.WebRootFileProvider);
var serveWorkbench = chronicleOptions.Features.Workbench && chronicleOptions.Features.Api && workbenchFileProvider is not null;
if (chronicleOptions.Features.Workbench && workbenchFileProvider is null)
{
    logger.WorkbenchUINotAvailable(app.Environment.WebRootPath ?? "<not set>");
}

var workbenchStaticFileOptions = new StaticFileOptions();

// Map workbench static files BEFORE authentication so they are publicly accessible
if (serveWorkbench)
{
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

// Where a certificate rotation stands: the ring this node loaded, and what the stored Data Protection keys
// still depend on. Authenticated, like everything that is not explicitly anonymous - it names key ids and
// certificate subjects, never key material. (#3690)
app.MapGet(
    "/diagnostics/encryption-certificates",
    (IEncryptionCertificateRotationDiagnostics diagnostics) => diagnostics.GetReport());

// Kernel state reset is exposed via the gRPC IServer.ResetKernelState operation, which
// only honours the call in DEVELOPMENT builds. See Cratis.Chronicle.Services.Host.Server.

// Map workbench fallback route AFTER API endpoints to avoid conflicts
if (serveWorkbench)
{
    app.MapFallbackToFile(WorkbenchUI.EntryPoint, workbenchStaticFileOptions).AllowAnonymous();
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

void LogCrash(Action<ILogger<Kernel>> log, Exception exception)
{
    if (logger is not null)
    {
        try
        {
            log(logger);

            return;
        }
        catch (Exception loggingFailure)
        {
            // A failure while routing the crash through the logging pipeline must not mask the
            // original exception - fall back to the console output below.
            Console.WriteLine(loggingFailure);
        }
    }

    Console.WriteLine("************ UNHANDLED PROCESS-LEVEL EXCEPTION ************");
    Console.WriteLine(exception);
}
