// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Registrations;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration.for_OldKernelOccurrences;

/// <summary>
/// Runs a released kernel independently of the current-image integration fixtures.
/// </summary>
public sealed class OldKernelFixture : IAsyncLifetime
{
    /// <summary>
    /// The capabilities were introduced by b5f008ed5b / ba51aae997, first tagged v19.69.0.
    /// v19.68.0 is the newest preceding release. Never build current source into this tag.
    /// </summary>
    public const string DefaultImage = "cratis/chronicle:19.68.0-development";
    readonly IContainer _kernel = new ContainerBuilder(Environment.GetEnvironmentVariable("CHRONICLE_OLD_KERNEL_IMAGE") ?? DefaultImage)
        .WithLabel("cratis.task", "4911-p3c-old-kernel")
        .WithPortBinding(35000, assignRandomHostPort: true)
        .WithEnvironment("Cratis__Chronicle__Authentication__Enabled", "false")
        .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new HttpsHealthWait(35000)))
        .Build();
    ChronicleClient? _client;
    HttpRpcObservation? _requests;

    public IEventStore EventStore { get; private set; } = null!;
    public HttpRpcObservation Requests => _requests!;

    public async Task InitializeAsync()
    {
        await _kernel.StartAsync();
        var port = _kernel.GetMappedPublicPort(35000);
        _requests = new(port);
        _client = new(
            new ChronicleOptions
            {
                ConnectionString = new ChronicleConnectionString($"chronicle://localhost:{port}/?skipTlsValidation=true"),
                ConnectTimeout = 60
            },
            new DefaultClientArtifactsProvider(new ClientAssemblyDiscovery()));
        EventStore = await _client.GetEventStore("old-kernel-occurrences").WaitAsync(TimeSpan.FromSeconds(90));
        var registration = await EventStore.WaitForRegistration().WaitAsync(TimeSpan.FromSeconds(90));
        registration.IsSuccess.ShouldBeTrue();
        var capabilities = (IKernelCapabilities)EventStore.Connection;
        capabilities.CapabilitiesAreKnown.ShouldBeTrue();
        capabilities.Capabilities.ShouldNotContain("occurrences");
        capabilities.Capabilities.ShouldNotContain("effect-receipts");
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _requests?.Dispose();
        await _kernel.DisposeAsync();
    }

    sealed class ClientAssemblyDiscovery : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [typeof(ChronicleClient).Assembly];
        public IEnumerable<Type> DefinedTypes => Assemblies.SelectMany(assembly => assembly.DefinedTypes).Select(type => type.AsType());
        public void Initialize() { }
    }
}
