// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// A projection whose persisted state says it is replaying, with no replay job left, reactivated while generation
/// initialization is quiescing — so the replay can be neither resumed nor started.
/// </summary>
public class a_replaying_projection_recovering_while_generation_initialization_is_quiescing : an_observer
{
    protected readonly RecordingObserverLogger _recordedLogs = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection, EventTypes = [EventType.Unknown] };
        await _definitionStorage.WriteStateAsync();
        _stateStorage.State = _stateStorage.State with
        {
            IsReplaying = true,
            RunningState = ObserverRunningState.Replaying,
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 43UL
        };
        await _stateStorage.WriteStateAsync();
        _jobsManager.GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>(
        [
            new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow),
            new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.LegacyWorkReconciled, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)
        ]));
        await Crash();
    }

    protected Task Recover() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero);

    protected bool ReleasedTheRefusedReplay => _recordedLogs.Warnings.Any(_ => _.StartsWith("Released a refused replay", StringComparison.Ordinal));

    protected bool RecordedTheRefusal => _recordedLogs.Warnings.Any(_ => _.Contains(GenerationInitializationReplayNotSupported.StartRefusedReason, StringComparison.Ordinal));
}
