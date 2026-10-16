// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Reconciles durable lifecycle intents at startup and only while work remains pending.
/// </summary>
/// <param name="storage">The kernel storage.</param>
/// <param name="logger">Reports recovery failures without failing startup or durable ingress.</param>
/// <param name="fanOutLogger">Reports individual intents left pending for retry.</param>
public class LifecycleOccurrences(IStorage storage, ILogger<LifecycleOccurrences> logger, ILogger<LifecycleFanOut> fanOutLogger) : Grain, ILifecycleOccurrences
{
    LifecycleFanOut _fanOut = null!;
    IGrainTimer? _timer;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var name = new EventStoreName(this.GetPrimaryKeyString());
        _fanOut = new(storage.GetEventStore(name), (scope, trigger) => OccurrenceQueueKey.Resolve(GrainFactory, name, scope, trigger).Prepare(), TimeProvider.System, fanOutLogger);
        Arm();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string> Record(OccurrenceKind kind, string activationId, bool allNamespaces)
    {
        var result = await _fanOut.Record(kind, activationId, allNamespaces);

        // The durable insert determines the reply. Recovery of any intent is independent of ingress.
        if (allNamespaces) Arm();
        return JsonSerializer.Serialize(result);
    }

    /// <inheritdoc/>
    public async Task Ensure()
    {
        Arm();
        try
        {
            if (!await _fanOut.Reconcile())
            {
                _timer?.Dispose();
                _timer = null;
            }
        }
        catch (Exception exception)
        {
            logger.FailedReconcilingLifecycleOccurrences(exception, this.GetPrimaryKeyString());
        }
    }

    void Arm() => _timer ??= this.RegisterGrainTimer(_ => Ensure(), new()
    {
        DueTime = TimeSpan.FromSeconds(1),
        Period = TimeSpan.FromSeconds(1),
        Interleave = false,
        KeepAlive = true
    });
}
