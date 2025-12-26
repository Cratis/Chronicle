// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography.X509Certificates;
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
using Microsoft.AspNetCore.Authorization;
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
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));
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

// Development certificate values are captured here so we can log about them after the host is built.
X509Certificate2? developmentServerCertificate = null;
string? developmentCaPem = null;

builder.WebHost.UseKestrel(options =>
{
#if DEVELOPMENT
    // In development mode: generate an in-memory dev CA + server cert and expose CA via well-known endpoint
    try
    {
        var dev = DevCertificateProvider.EnsureDevCertificate("localhost");
        developmentServerCertificate = dev.ServerCertificate;
        developmentCaPem = dev.CaPem;
    }
    catch
    {
        // fallback to existing loader if generation fails
        developmentServerCertificate = CertificateLoader.LoadCertificate(chronicleOptions);
    }
#else
    developmentServerCertificate = CertificateLoader.LoadCertificate(chronicleOptions);
#endif

    // Always listen on ManagementPort for API and well-known certificate endpoint
    options.ListenAnyIP(chronicleOptions.ManagementPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;

        if (!chronicleOptions.Tls.Disable)
        {
            if (developmentServerCertificate is not null)
            {
                listenOptions.UseHttps(developmentServerCertificate);
            }
            else
            {
                listenOptions.UseHttps();
            }
        }
    });

#if DEVELOPMENT
    // In development, also listen on TLS.DevelopmentCertificatePort for HTTP-only access to well-known endpoints
    // This allows clients to fetch the development CA without chicken-and-egg TLS problems
    options.ListenAnyIP(chronicleOptions.Tls.DevelopmentCertificatePort, listenOptions =>
    {
        // No TLS - HTTP only for fetching CA certificate
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
#endif

    options.ListenAnyIP(chronicleOptions.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;

        if (!chronicleOptions.Tls.Disable)
        {
            if (developmentServerCertificate is not null)
            {
                listenOptions.UseHttps(developmentServerCertificate);
            }
            else
            {
                listenOptions.UseHttps();
            }
        }
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
        .UseDashboard(options =>
        {
            options.Host = "*";
            options.Port = 8081;
            options.HostSelf = true;
        }))
   .ConfigureServices((context, services) =>
   {
       services.AddCodeFirstGrpc();
       services.AddCodeFirstGrpcReflection();

       services
          .AddBindingsByConvention()
          .AddChronicleTelemetry(context.Configuration)
          .AddSelfBindings()
          .AddGrpcServices()
          .AddSingleton(BinderConfiguration.Default);

       services.AddCodeFirstGrpc();

       // Add authentication services
       services.AddChronicleAuthentication(chronicleOptions);
   });

var app = builder.Build();

// Initialize default admin user if authentication is enabled
if (chronicleOptions.Authentication.Enabled)
{
    var authService = app.Services.GetRequiredService<IAuthenticationService>();
    await authService.EnsureDefaultAdminUser();

#if DEVELOPMENT
    // Ensure default client credentials for development
    await authService.EnsureDefaultClientCredentials();
#endif
}

app.UseRouting();

// Log about development certificate generation / exposure when available
try
{
    var logger = app.Services.GetRequiredService<ILogger<Kernel>>();

    if (developmentServerCertificate is not null && !string.IsNullOrEmpty(developmentCaPem))
    {
        logger.GeneratedDevelopmentCertificate();
    }
}
catch (Exception ex)
{
    // Swallow logging errors to avoid bringing down the host; still write to console for visibility
    Console.WriteLine($"Failed to log development certificate info: {ex.Message}");
}

#if DEVELOPMENT
app.UseDeveloperExceptionPage();
#endif

app.UseCratisArc();
app.UseRouting();

// Add authentication and authorization middleware AFTER routing but BEFORE endpoints
if (chronicleOptions.Authentication.Enabled)
{
    app.UseMiddleware<GrpcAuthenticationMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
}

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
if (chronicleOptions.Authentication.Enabled)
{
    app.MapGroup("/identity")
        .MapIdentityApi<ChronicleUser>()
        .AllowAnonymous();
}

// Map controllers for API and OAuth
if (chronicleOptions.Features.Api || chronicleOptions.Features.OAuthAuthority)
{
    app.MapControllers();
}

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

#if DEVELOPMENT
app.MapGet("/.well-known/chronicle/ca", (ILogger<Kernel> logger) =>
{
    // Log and return the in-memory development CA if available.
    if (!string.IsNullOrEmpty(developmentCaPem))
    {
        logger.ServingDevelopmentCa(chronicleOptions.Tls.DevelopmentCertificatePort);
        return Results.Text(developmentCaPem, "application/x-pem-file");
    }

    // As a fallback, attempt to materialize a development cert (DEVELOPMENT only) or return empty.
    try
    {
        var cert = DevCertificateProvider.EnsureDevCertificate();
        logger.ServingDevelopmentCa(chronicleOptions.Tls.DevelopmentCertificatePort);
        return Results.Text(cert.CaPem ?? string.Empty, "application/x-pem-file");
    }
    catch (Exception ex)
    {
        logger.FailedServingDevelopmentCa(ex);
        return Results.Text(string.Empty, "application/x-pem-file");
    }
}).AllowAnonymous();
#endif

using var cancellationToken = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("******* SHUTTING DOWN CHRONICLE SERVER *******");
    cancellationToken.Cancel();
    eventArgs.Cancel = true;
};

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
