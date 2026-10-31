// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_duplicate_claims_are_reported : for_ReindexConstraintsStep.given.a_reindex_with_duplicate_claims
{
    ReindexConstraintsStepState _reported;

    async Task Because()
    {
        await Perform();
        _reported = await _step.GetState();
    }

    [Fact] void should_complete_the_scan()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetError(out _).ShouldBeFalse();
    }
    [Fact] void should_report_only_the_constraint_name() => _reported.CollisionCounts.Keys.ShouldContainOnly(_unique.Name);
    [Fact] void should_count_each_skipped_claim() => _reported.CollisionCounts[_unique.Name].ShouldEqual(2L);
    [Fact] void should_keep_both_samples() => _reported.CollisionSamples[_unique.Name].Count.ShouldEqual(2);
    [Fact] void should_hash_identical_claims_identically() => _reported.CollisionSamples[_unique.Name][0].ValueHash.ShouldEqual(_reported.CollisionSamples[_unique.Name][1].ValueHash);
    [Fact] void should_keep_the_skipped_sequence_numbers() => _reported.CollisionSamples[_unique.Name].Select(sample => sample.SequenceNumber).ShouldEqual([new EventSequenceNumber(1), new EventSequenceNumber(2)]);
    [Fact] void should_not_expose_the_unsalted_value_hash_or_index_value() => _reported.CollisionSamples[_unique.Name][0].ValueHash.ShouldNotEqual(_hash.Value);
    [Fact] void should_use_a_sha256_length_hash() => Convert.FromHexString(_reported.CollisionSamples[_unique.Name][0].ValueHash).Length.ShouldEqual(32);
    [Fact] void should_use_lowercase_hexadecimal() => _reported.CollisionSamples[_unique.Name][0].ValueHash.ShouldEqual(_reported.CollisionSamples[_unique.Name][0].ValueHash.ToLowerInvariant());
    [Fact] void should_not_overflow_below_the_cap() => _reported.CollisionSampleOverflowCounts.ShouldBeEmpty();
    [Fact] void should_surface_only_names_and_counts_in_progress() => _reported.Progress.Message.Value.ShouldEqual("Skipped 2 duplicate claims: UniqueEmail=2");
    [Fact] async Task should_keep_the_first_claimant() => (await _claims.IsAllowed("following", _unique, _hash)).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_continue_indexing_after_the_collisions() => await _uniqueStorage.Received(1).Save("following", _unique, new EventSequenceNumber(3), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
}
