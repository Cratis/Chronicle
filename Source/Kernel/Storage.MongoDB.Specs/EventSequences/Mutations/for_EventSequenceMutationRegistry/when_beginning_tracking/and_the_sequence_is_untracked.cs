// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_tracking;

/// <summary>
/// Beginning tracking for an event sequence with no head document yet must upsert one and move its coverage to
/// unsealed - a non-existent document is logically <see cref="EventSequenceMutationCoverage.Untracked"/>.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_sequence_is_untracked(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationTrackingResult _result = default!;

    async Task Because() => _result = await Registry.BeginTracking(Target, EventSequenceMutationCoverage.Untracked);

    [Fact] void should_begin_tracking() => _result.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.Began);
    [Fact] void should_report_unsealed_coverage() => _result.Coverage.ShouldEqual((EventSequenceMutationCoverage?)EventSequenceMutationCoverage.Unsealed);
}
