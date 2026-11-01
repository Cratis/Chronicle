// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;
extern alias KernelConcepts;

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConnectionService = KernelGrpc::Cratis.Chronicle.Services.Clients.ConnectionService;
using IConnectedClients = KernelCore::Cratis.Chronicle.Clients.IConnectedClients;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;
using OpenConnections = KernelGrpc::Cratis.Chronicle.Services.Clients.OpenConnections;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for Orleans in-process.
/// </summary>
/// <remarks>
/// It advertises the in-process kernel's own capabilities, taken from the same compatibility check the gRPC
/// endpoint serves, so integration specs exercise the capability routes a current production kernel would take.
/// A spec can withhold a capability for its own lifetime to exercise the fallback route.
/// <para>
/// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
/// </para>
/// </remarks>
/// <param name="lifecycle"><see cref="IConnectionLifecycle"/> for managing lifecycle.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for the silo the client is co-hosted with.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
internal class ChronicleConnection(
    IConnectionLifecycle lifecycle,
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    ILoggerFactory loggerFactory) : IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities
{
    /// <summary>
    /// In-process test clients share the silo process and cannot network-drop, so they are flagged as
    /// keep-alive-exempt to stop the kernel disconnecting them on the (cross-silo) keep-alive timeout.
    /// </summary>
    const bool KeepAliveExempt = true;

    readonly object _capabilitiesLock = new();
    IServices? _services;
    ConnectionService? _connectionService;
    string[] _advertised = [];
    string[] _withheld = [];
    string[] _capabilities = [];
    bool _capabilitiesKnown;

    /// <inheritdoc/>
    IConnectionLifecycle IChronicleConnection.Lifecycle => lifecycle;

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected().GetAwaiter().GetResult();
            return _services!;
        }
    }

    /// <inheritdoc/>
    IReadOnlyCollection<string> IKernelCapabilities.Capabilities
    {
        get
        {
            EnsureCapabilities();
            return _capabilities;
        }
    }

    /// <inheritdoc/>
    bool IKernelCapabilities.CapabilitiesAreKnown
    {
        get
        {
            EnsureCapabilities();
            return _capabilitiesKnown;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lifecycle.Disconnected().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    Task IChronicleConnection.Connect() => Connect();

    /// <summary>
    /// Set the services.
    /// </summary>
    /// <param name="services">Services to set.</param>
    internal void SetServices(IServices services) => _services = services;

    /// <summary>
    /// Re-establishes the client connection after a lifecycle disconnect.
    /// Registers with the <c language="csharp">ConnectedClients</c> grain, re-creates the keep-alive
    /// stream, and signals <see cref="IConnectionLifecycle.Connected"/> which triggers
    /// <c language="csharp">RegisterAll</c> on the <see cref="IEventStore"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Reconnect() => Connect();

    /// <summary>
    /// Withholds capabilities from the advertised set until the returned scope is disposed, so a spec can
    /// exercise the route a kernel without them would force. The withholding survives a reconnect.
    /// </summary>
    /// <param name="capabilities">The capabilities to withhold.</param>
    /// <returns>An <see cref="IDisposable"/> that restores the previous withholding when disposed.</returns>
    internal IDisposable WithholdCapabilities(params string[] capabilities)
    {
        string[] previous;
        lock (_capabilitiesLock)
        {
            previous = _withheld;
            _withheld = [.. previous.Union(capabilities)];
            Recompute();
        }

        return new Withholding(() =>
        {
            lock (_capabilitiesLock)
            {
                _withheld = previous;
                Recompute();
            }
        });
    }

    void EnsureCapabilities()
    {
        if (!_capabilitiesKnown && !lifecycle.IsConnected)
        {
            ConnectIfNotConnected().GetAwaiter().GetResult();
        }
    }

    void Recompute() => _capabilities = [.. _advertised.Except(_withheld)];

    async Task FetchCapabilities()
    {
        _capabilitiesKnown = false;
        var response = await _connectionService!.CheckCompatibility(new CompatibilityRequest
        {
            ClientType = ".NET",
            ClientVersion = typeof(ChronicleConnection).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            ProtocolVersion = Contracts.ProtocolVersion.Current,
            DescriptorSet = Contracts.WireContractDescriptorSet.Bytes.ToArray()
        });

        if (!response.IsCompatible)
        {
            throw new IncompatibleServerException(
                $"The in-process kernel {response.ServerVersion} (protocol {response.ServerProtocolVersion}) is incompatible with this client: {string.Join("; ", response.Incompatibilities)}");
        }

        lock (_capabilitiesLock)
        {
            _advertised = [.. response.Capabilities];
            Recompute();
            _capabilitiesKnown = true;
        }
    }

    async Task ConnectIfNotConnected()
    {
        if (!lifecycle.IsConnected)
        {
            await Connect();
        }
    }

    async Task Connect()
    {
        // Register the client connection with the ConnectedClients grain BEFORE
        // signaling connected. The OnConnected callbacks (e.g. RegisterAll) register
        // observers whose grains call GetConnectedClient(). If the client hasn't been
        // registered yet, that call throws ClientIsNotConnected and the observer
        // silently fails to subscribe — causing flaky WaitForState timeouts.
        // The production ChronicleConnection avoids this by waiting for the first
        // keep-alive (which only arrives after OnClientConnected completes).
        var connectedClients = grainFactory.GetGrain<IConnectedClients>(localSiloDetails.SiloAddress.ToParsableString());
        await connectedClients.OnClientConnected(
            (KernelConnectionId)lifecycle.ConnectionId.Value,
            string.Empty,
            KeepAliveExempt,
            Environment.ProcessId,
            Environment.ProcessPath ?? string.Empty,
            Environment.MachineName,
            ".NET");

        var chronicleOptions = Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions());
        _connectionService = new ConnectionService(
            grainFactory,
            localSiloDetails,
            new KernelCore::Cratis.Chronicle.Clients.ConnectedClientsQuery(grainFactory, chronicleOptions),
            new OpenConnections(),
            loggerFactory.CreateLogger<ConnectionService>(),
            chronicleOptions);
        _connectionService.Connect(new()
        {
            ConnectionId = lifecycle.ConnectionId,
            IsRunningWithDebugger = KeepAliveExempt,
            ClientType = ".NET",
        }).Subscribe(HandleConnection);

        // The capabilities must be known before the OnConnected callbacks fire, because they read them.
        await FetchCapabilities();
        await lifecycle.Connected();
    }

    void HandleConnection(ConnectionKeepAlive keepAlive) => _ = SendKeepAlive();

    async Task SendKeepAlive()
    {
        // Fire-and-forget and swallow transient failures. The kernel emits keep-alives on a 1s loop and
        // terminates that loop — disconnecting the client — if delivering one throws. Each ping is a
        // cross-silo grain call here, so a single transient rejection or slow call must not propagate back
        // into the emission loop (which the previous synchronous GetResult() did) nor block it.
        try
        {
            var connectionId = (KernelConnectionId)lifecycle.ConnectionId.Value;
            var connectedClients = grainFactory.GetGrain<IConnectedClients>(localSiloDetails.SiloAddress.ToParsableString());
            var stillConnected = await connectedClients.OnClientPing(connectionId);

            // The ConnectedClients grain keeps its registry in memory only. In a cluster it can reactivate
            // (or migrate silos) and lose that registry, after which OnClientPing reports the client as
            // unknown and observers start failing with "Subscriber is disconnected". Re-register so the
            // grain learns about this still-alive client again and observers stay subscribed. The
            // lifecycle.IsConnected guard is essential: a test that explicitly disconnects via
            // Lifecycle.Disconnected() must stay disconnected — without it this heartbeat would silently
            // resurrect the connection and break reconnect specs.
            if (!stillConnected && lifecycle.IsConnected)
            {
                await connectedClients.OnClientConnected(connectionId, string.Empty, KeepAliveExempt, Environment.ProcessId, Environment.ProcessPath ?? string.Empty, Environment.MachineName, ".NET");
            }
        }
        catch
        {
        }
    }

    sealed class Withholding(Action restore) : IDisposable
    {
        int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                restore();
            }
        }
    }
}
