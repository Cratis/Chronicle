// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Server.Authentication;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Storage;

namespace Cratis.Chronicle.Server.for_KernelHost.given;

public class a_held_silo : Specification
{
    protected WebApplication _app;
    protected Task _start;
    protected HttpClient _client;
    protected CancellationTokenSource _startup;
    protected int _mainPort;
    protected int _healthPort;
    internal held_active_stage _hold;
    protected virtual bool AuthenticationEnabled => false;
    protected virtual bool ControllersBeforeSilo => false;
    protected virtual bool ConcurrentStartup => false;
    protected virtual bool DiscoverConventions => false;

    void Establish()
    {
        _mainPort = FreePort();
        _healthPort = FreePort();
        _startup = new();
        _hold = new();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, _mainPort, listen => listen.Protocols = HttpProtocols.Http2);
            options.Listen(IPAddress.Loopback, _healthPort, listen => listen.Protocols = HttpProtocols.Http1);
        });
        if (DiscoverConventions)
        {
            builder.Services.AddBindingsByConvention().AddSelfBindings();
            builder.Services.Any(_ => _.ServiceType == typeof(DataProtectionKeyRingWarmup)).ShouldBeFalse();
        }

        if (ControllersBeforeSilo) builder.Services.AddControllers();
        builder.Services.Configure<HostOptions>(options => options.ServicesStartConcurrently = ConcurrentStartup);
        var systemStorage = new Storage.InMemory.SystemStorage();
        var storage = Substitute.For<IStorage>();
        storage.System.Returns(systemStorage);
        builder.Host.UseOrleans(silo => silo
            .UseLocalhostClustering(FreePort(), FreePort(), serviceId: Guid.NewGuid().ToString(), clusterId: Guid.NewGuid().ToString())
            .Services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.DataProtectionKeys, (_, _) => new DataProtectionKeysStorageProvider(storage)));
        builder.Services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(_hold);
        var chronicleOptions = new ChronicleOptions
        {
            Health = new Health { Port = _healthPort, Exclusive = true },
            Storage = new Configuration.Storage { Type = StorageType.InMemory },
            Authentication = new Configuration.Authentication { Enabled = AuthenticationEnabled, Authority = "https://identity.example" }
        };
        builder.Services.AddSingleton(Options.Create(chronicleOptions));
        builder.Services.AddSingleton<ISystemStorage>(systemStorage);
        if (!ControllersBeforeSilo) builder.Services.AddControllers();
        builder.Services.AddChronicleAuthentication(chronicleOptions);
        if (DiscoverConventions)
        {
            builder.Services.AddChronicleDataProtection();
            builder.Services.AddChronicleDataProtection();
        }
        builder.Services.AddSingleton<IInitialAdminSetupTokens, InitialAdminSetupTokens>();
        builder.Services.UseDeferredSiloStart();
        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            if (HealthOnlyPortPolicy.ShouldReject(new ChronicleOptions { Health = new Health { Port = _healthPort, Exclusive = true } }, context.Connection.LocalPort, context.Request.Path))
            {
                context.Response.StatusCode = 404;
                return;
            }

            await next(context);
        });
        _app.UseMiddleware<KernelStartupGate>();
        _app.MapKernelHealthChecks("/health");
        _app.MapGet("/workbench", () => "workbench");
        _client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{_healthPort}"), Timeout = TimeSpan.FromSeconds(10) };
    }

    protected async Task StartHeld()
    {
        _start = _app.StartAsync(_startup.Token);
        await _hold.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    async Task Destroy()
    {
        await _startup.CancelAsync();
        _hold.Release.TrySetResult();
        try
        {
            if (_start is not null) await _start.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (OperationCanceledException)
        {
            // Cancellation during the Active stage is an orderly requested stop.
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _app.StopAsync(timeout.Token);
            await _app.DisposeAsync();
            _client.Dispose();
            _startup.Dispose();
        }
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
