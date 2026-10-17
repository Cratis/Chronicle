// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Records host lifecycle boundaries without treating instance scale-in as logical shutdown.
/// </summary>
/// <param name="client">The host's client.</param>
/// <param name="artifacts">The host's artifact catalog.</param>
/// <param name="lifetime">The host lifetime.</param>
/// <param name="hostOptions">The host's shutdown deadline.</param>
/// <param name="logger">The lifecycle logger.</param>
internal sealed class OccurrenceLifecycle(
    IChronicleClient client,
    IClientArtifactsProvider artifacts,
    IHostApplicationLifetime lifetime,
    IOptions<HostOptions> hostOptions,
    ILogger<OccurrenceLifecycle> logger) : IHostedService, IDisposable
{
    readonly object _sync = new();
    CancellationTokenRegistration _stopping;
    CancellationTokenSource? _deadline;
    Task? _shutdown;
    Occurrences? _occurrences;

    ChronicleClientOptions Options => client.Options as ChronicleClientOptions ?? throw new TriggerNotValid("Host lifecycle recording requires ChronicleClientOptions with an event store name.");

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hasLifecycle = artifacts.OccurrenceHandlers.SelectMany(_ => _.GetInterfaces()).Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IOccurrenceHandler<>) && TriggerRegistry.IsLifecycle(_.GenericTypeArguments[0]));
        if (!hasLifecycle) return;
        LifecycleActivation.Resolve(Options);
        var store = await client.GetEventStore(Options.EventStore).WaitAsync(cancellationToken);
        var registration = await store.WaitForRegistration().WaitAsync(cancellationToken);
        if (!registration.IsSuccess) throw new TriggerNotValid("Lifecycle startup requires successful artifact registration.");
        _occurrences = store.Occurrences as Occurrences ?? throw new TriggerNotValid("The host's occurrence service does not support lifecycle recording.");
        await _occurrences.StartActivation(cancellationToken);
        if (Options.ActivationScope == ActivationScope.PerProcess) _stopping = lifetime.ApplicationStopping.Register(BeginStop);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_occurrences is null || Options.ActivationScope != ActivationScope.PerProcess) return;
        BeginStop();
        await _shutdown!.WaitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stopping.Dispose();
        _deadline?.Cancel();
        _deadline?.Dispose();
    }

    void BeginStop()
    {
        lock (_sync)
        {
            if (_shutdown is not null) return;
            _deadline = new(hostOptions.Value.ShutdownTimeout);
            _shutdown = RunStop(_deadline.Token);
        }
    }

    async Task RunStop(CancellationToken cancellationToken)
    {
        try
        {
            await _occurrences!.StopActivation(cancellationToken);
        }
        catch (Exception exception)
        {
            // Shutdown is best effort, not an acknowledgement of a durable write or successful cleanup.
            logger.LifecycleShutdownIncomplete(exception);
        }
    }
}
