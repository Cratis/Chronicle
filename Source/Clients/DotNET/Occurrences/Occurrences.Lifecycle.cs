// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Threading.Channels;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

public partial class Occurrences : IDisposable, IAsyncDisposable
{
    readonly object _workerSync = new();
    readonly Channel<bool> _scopeChanges = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    readonly HashSet<string> _lifecycleNamespaces = new(StringComparer.Ordinal);
    Func<Type, Type, OccurrenceDeliveryScope?, OccurrenceAttempt>? _attemptFactory;
    ChronicleOptions _options = new();
    ILogger<Occurrences>? _logger;
    Action? _cancelWorkers;
    Task _workers = Task.CompletedTask;
    bool _disposed;
    long _epoch;
    long? _workerEpoch;

    /// <summary>
    /// Gets or sets the clock for renewal and stream reconnection delays.
    /// </summary>
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_workerSync)
        {
            if (_disposed) return;
            _disposed = true;
            eventStore.Connection.Lifecycle.OnDisconnected -= Disconnect;
            _cancelWorkers?.Invoke();
        }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Task workers;
        lock (_workerSync)
        {
            _disposed = true;
            eventStore.Connection.Lifecycle.OnDisconnected -= Disconnect;
            workers = _workers;
        }
        await Disconnect();
        await workers;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Attaches trigger discovery and attempt services to the owning store.
    /// </summary>
    /// <param name="artifacts">The store-local discovery provider.</param>
    /// <param name="schemas">The schema generator.</param>
    /// <param name="jsonOptions">The payload serializer options.</param>
    /// <param name="services">The root services for attempt scopes.</param>
    /// <param name="activator">The artifact activator.</param>
    /// <param name="causation">The causation context.</param>
    /// <param name="identity">The identity context.</param>
    /// <param name="logger">The worker logger.</param>
    /// <param name="options">Optional lifecycle configuration.</param>
    internal void Configure(
        IClientArtifactsProvider artifacts,
        IJsonSchemaGenerator schemas,
        JsonSerializerOptions jsonOptions,
        IServiceProvider services,
        IClientArtifactsActivator activator,
        ICausationManager causation,
        IIdentityProvider identity,
        ILogger<Occurrences> logger,
        ChronicleOptions? options = null)
    {
        _options = options ?? new();
        _triggers = new(eventStore, artifacts, schemas);
        _jsonOptions = jsonOptions;
        _logger = logger;
        _attemptFactory = (trigger, handler, scope) => new(
            eventStore,
            ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences,
            trigger,
            handler,
            services,
            activator,
            jsonOptions,
            causation,
            identity,
            TimeProvider,
            logger,
            scope);
        eventStore.Connection.Lifecycle.OnDisconnected += Disconnect;
    }

    /// <summary>
    /// Prepares a complete trigger catalog before registration effects begin.
    /// </summary>
    /// <returns>The local discovery work.</returns>
    internal async Task Discover()
    {
        await Disconnect();
        await _workers;
        _triggers?.Discover();
        if (_triggers?.HasLifecycleHandlers == true) LifecycleActivation.Resolve(_options);
    }

    /// <summary>
    /// Registers the prepared schemas before attaching a single worker per trigger.
    /// </summary>
    /// <returns>The registration work.</returns>
    internal async Task Register()
    {
        long epoch;
        lock (_workerSync)
        {
            if (_disposed || _triggers is null) return;
            epoch = _epoch;
        }
        await _triggers.Register();
        lock (_workerSync)
        {
            if (_disposed || epoch != _epoch || _workerEpoch == epoch) return;
            if (!_triggers.Handlers.Any()) return;
            _workerEpoch = epoch;
            _workers = RunWorkers(_workers, epoch);
        }
    }

    /// <summary>
    /// Discovers the in-process scenario's trigger catalog and registers it on ingress, without workers or lifecycle work.
    /// Receipt-only access must not depend on occurrence ingress support.
    /// </summary>
    internal void InitializeForTesting()
    {
        _triggers?.Discover();
        _registerForTesting = _triggers is null ? null : _triggers.Register;
    }

    async Task RunWorkers(Task previous, long epoch)
    {
        // Reconnect registration must not join user code in the connection callback, but a new
        // session must wait until the old handlers have released their scopes and subscriptions.
        await previous;
        using var lifetime = new CancellationTokenSource();
        lock (_workerSync)
        {
            if (_disposed || epoch != _epoch) return;
            _cancelWorkers = lifetime.Cancel;
        }
        try
        {
            await RunScopedWorkers(lifetime.Token);
        }
        finally
        {
            lock (_workerSync)
            {
                if (_epoch == epoch)
                {
                    _cancelWorkers = null;
                    _workerEpoch = null;
                }
            }
        }
    }

    Task Disconnect()
    {
        CancelWorkers();

        // Never join handlers from the connection callback: the handler may itself be awaiting that connection.
        return Task.CompletedTask;
    }

    void CancelWorkers()
    {
        lock (_workerSync)
        {
            _epoch++;
            _cancelWorkers?.Invoke();
            _cancelWorkers = null;
            _workerEpoch = null;
        }
    }
}
