// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Cratis.Arc.MongoDB;
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
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
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
KernelStartupDiagnostics.ReportIdentityProviderCertificate(chronicleOptions, logger);
ForwardedHeadersTrust.Configure(builder.Services, chronicleOptions);
var isSqlStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
    || string.Equals(chronicleOptions.Storage.Type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
    || string.Equals(chronicleOptions.Storage.Type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase);
var isInMemoryStorage = string.Equals(chronicleOptions.Storage.Type, StorageType.InMemory, StringComparison.OrdinalIgnoreCase);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

if (chronicleOptions.Features.Api)
{
    builder.Services.AddChronicleWorkbenchApi();
}

#if DEVELOPMENT
const bool developmentBuild = true;
#else
const bool developmentBuild = false;
#endif

// Kestrel keeps the selected certificate alive for every TLS handshake until process shutdown.
var certificate = KernelCertificateSelection.Select(chronicleOptions, logger, developmentBuild);

var listeners = new List<(int Port, HttpProtocols Protocols, X509Certificate2? Certificate)>();
KernelListeners.Configure(chronicleOptions, certificate, logger, (port, protocols, listenerCertificate) =>
    listeners.Add((port, protocols, listenerCertificate)));
logger.ServerListening(chronicleOptions.Port);

builder.Services.AddOptions<SocketTransportOptions>().Configure<IHostApplicationLifetime>((options, lifetime) =>
    options.CreateBoundListenSocket = endpoint => ResilientListenSocket.Bind(endpoint, chronicleOptions.BindTimeout, logger, lifetime.ApplicationStopping));

builder.WebHost.UseKestrel(options =>
{
    foreach (var listener in listeners)
    {
        options.ListenAnyIP(listener.Port, listenOptions =>
        {
            listenOptions.Protocols = listener.Protocols;
            if (listener.Certificate is not null)
            {
                listenOptions.UseHttps(listener.Certificate);
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
    options.GeneratedApis.SegmentsToSkipForRoute = 2;
})
.AddCratisMongoDB(
   configureOptions: mongo =>
   {
       if (!isSqlStorage && !isInMemoryStorage)
       {
           mongo.Server = chronicleOptions.Storage.ConnectionDetails;
           mongo.Database = Cratis.Chronicle.Storage.MongoDB.DatabaseNames.WithPrefix(
               Cratis.Chronicle.Storage.MongoDB.WellKnownDatabaseNames.Chronicle,
               chronicleOptions.Storage.DatabaseNamePrefix);
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

        // Applies to both clustering types, and to calls a silo makes to itself as well as to a
        // sibling. Startup work that fans out across everything the server holds grows with the
        // deployment while Orleans' 30 second default does not, so this is the knob that lets an
        // operator get a server that has outgrown it to start again.
        //
        // All three have to be set, and SystemResponseTimeout is the one that actually governs the
        // call this exists for. A grain service is a system target, and Orleans times system target
        // calls out against SystemResponseTimeout rather than ResponseTimeout - so setting only the
        // latter leaves the startup path that needs the larger budget still on the 30 second
        // default while appearing to have been configured. The startup task also reaches its grain
        // services through the client the silo hosts for itself, which is bound by the client's own
        // timeout, hence all three.
        _.Configure<Orleans.Configuration.SiloMessagingOptions>(options =>
        {
            options.ResponseTimeout = clustering.ResponseTimeout;
            options.SystemResponseTimeout = clustering.ResponseTimeout;
        });
        _.Configure<Orleans.Configuration.ClientMessagingOptions>(options => options.ResponseTimeout = clustering.ResponseTimeout);

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

// Accept the public scheme and client IP only from loopback or explicitly trusted reverse proxies.
// ASPNETCORE_FORWARDEDHEADERS_ENABLED inserts its own middleware first; use Chronicle's policy
// there and avoid inserting a second one that could consume another forwarded hop.
ForwardedHeadersTrust.Use(app, builder.Configuration);

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

// Add authentication and authorization middleware AFTER routing but BEFORE endpoints. With authentication off
// there are no schemes registered for UseAuthentication to resolve, and the fallback policy authorizes
// everything, so both are skipped rather than run against an empty stack.
if (chronicleOptions.Authentication.Enabled)
{
    app.UseMiddleware<GrpcAuthenticationMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CookieAntiforgeryMiddleware>();
}

if (chronicleOptions.Features.Api)
{
    // Configure API endpoints but without calling UseRouting again (already called above)
    app.UseWebSockets();
    app.UseSwagger();
    app.UseSwaggerUI(WorkbenchApiExtensions.ConfigureSwaggerUI);
}

// Map Identity API endpoints for SPA authentication - MUST be before MapControllers. They are backed by the
// ASP.NET Identity stack, which is not registered when authentication is off.
if (chronicleOptions.Authentication.Enabled)
{
    app.MapGet("/.cratis/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        new AntiforgeryTokenResponse(antiforgery.GetAndStoreTokens(context).RequestToken!));

    IdentityEndpointAuthorization.Apply(app.MapGroup("/identity").MapIdentityApi<User>());
    app.MapPost("/identity/logout", async (SignInManager<User> signInManager) =>
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    });
}
else
{
    // Explicitly advertise disabled request protection rather than making the Workbench guess from a 404.
    app.MapGet("/.cratis/antiforgery", () => Results.NoContent()).AllowAnonymous();
}

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

// Announced from ApplicationStarted, not from here. Kestrel binds during RunAsync, so logging it before the call
// made the message arrive around half a second before the port accepted anything - and entrypoint scripts, test
// fixtures and orchestrators that gate on this line would then connect to a socket that did not exist yet.
app.Lifetime.ApplicationStarted.Register(() => logger.ServerStarted(chronicleOptions.Port));

try
{
    await app.RunAsync(cancellationToken.Token);
}
catch (OperationCanceledException)
{
    // A shutdown signal (SIGTERM, Ctrl+C) that arrives while the host is still starting - e.g. Kestrel is
    // still binding, or an ILifecycleParticipant is still running - cancels the host's own lifetime token
    // and surfaces as an OperationCanceledException out of RunAsync instead of RunAsync completing
    // normally. Left uncaught, that reaches AppDomain.CurrentDomain.UnhandledException and the process
    // exits as an unhandled crash for what is actually an orderly, requested stop. (#3936)
    logger.ServerShutdownDuringStartup();
}

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
