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
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.Security;
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

var serverCertificate = CertificateLoader.LoadCertificate(chronicleOptions);

if (serverCertificate is not null)
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
    // Always listen on ManagementPort for API
    options.ListenAnyIP(chronicleOptions.ManagementPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;

        if (serverCertificate is not null)
        {
            listenOptions.UseHttps(serverCertificate);
        }
#if !DEVELOPMENT
        else
        {
            throw new InvalidOperationException("No TLS certificate is configured. Please provide a certificate path in configuration.");
        }
#endif
    });

    options.ListenAnyIP(chronicleOptions.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;

        if (serverCertificate is not null)
        {
            listenOptions.UseHttps(serverCertificate);
        }
#if !DEVELOPMENT
        else
        {
            throw new InvalidOperationException("No TLS certificate is configured. Please provide a certificate path in configuration.");
        }
#endif
    });

    options.Limits.Http2.MaxStreamsPerConnection = 100;
});

builder.Host
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
           mongo.Server = chronicleOptions.Storage.ConnectionDetails;
           mongo.Database = WellKnownDatabaseNames.Chronicle;
       },
       builder => builder.WithCamelCaseNamingPolicy())
   .UseOrleans(_ => _
        .UseLocalhostClustering()
        .AddChronicleToSilo(_ => _
           .WithMongoDB(chronicleOptions))
        .AddStartupTask<AuthenticationStartupTask>())
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
   });

var app = builder.Build();

logger = app.Services.GetRequiredService<ILogger<Kernel>>();
logger.ServerConfigured();

app.UseRouting();

app.UseCratisArc();
app.UseRouting();

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
    .MapIdentityApi<ChronicleUser>()
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

// Map workbench static files and fallback AFTER API endpoints to avoid conflicts
if (chronicleOptions.Features.Workbench && chronicleOptions.Features.Api)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
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
