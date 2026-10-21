// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Records an effect that appends no events and has no protected decision scopes.
/// </summary>
/// <remarks>
/// Always calls the Log grain. An AlreadyRecorded hit returns the namespace-scoped receipt but does
/// not reconcile another event sequence's grain state; append retries reconcile their own sequence.
/// </remarks>
/// <param name="EventStore">The target event store.</param>
/// <param name="Namespace">The target namespace.</param>
/// <param name="EffectReceipt">The occurrence effect and optional response.</param>
/// <param name="Outcome">NoEvents or Refused.</param>
/// <param name="Reasons">The refusal reasons.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record RecordEffectReceipt(EventStoreName EventStore, EventStoreNamespaceName Namespace, EffectReceipt EffectReceipt, EffectOutcome Outcome, IEnumerable<string> Reasons)
{
    /// <summary>
    /// Records the outcome through the event sequence grain, without concurrency scopes.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The immutable winning receipt and recording status.</returns>
    public Task<RecordEffectReceiptResult> Handle(IGrainFactory grainFactory) =>
        grainFactory.GetEventSequence(EventSequenceId.Log, EventStore, Namespace).RecordEffectReceipt(EffectReceipt, Outcome, Reasons.ToArray());
}
