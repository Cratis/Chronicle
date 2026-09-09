// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_tracking;

/// <summary>
/// Beginning tracking a second time, once the sequence is already unsealed, must be idempotent rather than
/// reporting a conflict - matching the in-memory registry's tolerance for a repeated begin-tracking call.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_sequence_is_already_unsealed(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationTrackingResult _result = default!;

    async Task Because()
    {
        await Registry.BeginTracking(Target, EventSequenceMutationCoverage.Untracked);
        _result = await Registry.BeginTracking(Target, EventSequenceMutationCoverage.Untracked);
    }

    [Fact] void should_report_tracking_as_already_active() => _result.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.AlreadyTracking);
    [Fact] void should_still_report_unsealed_coverage() => _result.Coverage.ShouldEqual((EventSequenceMutationCoverage?)EventSequenceMutationCoverage.Unsealed);
}
