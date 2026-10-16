// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Reconciles durable lifecycle intents at startup and periodically while the store is active.
/// </summary>
/// <param name="storage">The kernel storage.</param>
public class LifecycleOccurrences(IStorage storage) : Grain, ILifecycleOccurrences
{
    LifecycleFanOut _fanOut = null!;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var name = new EventStoreName(this.GetPrimaryKeyString());
        _fanOut = new(storage.GetEventStore(name), (scope, trigger) => OccurrenceQueueKey.Resolve(GrainFactory, name, scope, trigger).Prepare(), TimeProvider.System);
        this.RegisterGrainTimer(_ => _fanOut.Reconcile(), new() { DueTime = TimeSpan.FromSeconds(1), Period = TimeSpan.FromSeconds(1), Interleave = false, KeepAlive = true });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string> Record(OccurrenceKind kind, string activationId, bool allNamespaces)
    {
        var result = await _fanOut.Record(kind, activationId, allNamespaces);
        await _fanOut.Reconcile();
        return JsonSerializer.Serialize(result);
    }

    /// <inheritdoc/>
    public Task Ensure() => _fanOut.Reconcile();
}
