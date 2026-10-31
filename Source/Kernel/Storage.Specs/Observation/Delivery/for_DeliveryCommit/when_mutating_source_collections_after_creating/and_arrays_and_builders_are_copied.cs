// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommit.when_mutating_source_collections_after_creating;

public class and_arrays_and_builders_are_copied : for_DeliveryCommitValidator.given.a_valid_commit
{
    ParkedKeyOperation[] _parkedSource;
    ImmutableArray<SkipRangeOperation>.Builder _skipSource;
    DeliveryLane[] _laneSource;
    DeliveryCommit _captured;
    DeliveryCommit _equal;

    void Establish()
    {
        _parkedSource = [ParkedKeyOperation.Insert(_parked)];
        _skipSource = ImmutableArray.CreateBuilder<SkipRangeOperation>();
        _skipSource.Add(SkipRangeOperation.Insert(_skip));
        _laneSource = [.. _root.Lanes];
        var root = WithLanes(_root, ImmutableCollectionsMarshal.AsImmutableArray(_laneSource));
        _captured = new(DeliveryRevision.None, root, _parkedSource, _skipSource);
        _equal = Create(_root, [ParkedKeyOperation.Insert(_parked)], [SkipRangeOperation.Insert(_skip)]);
    }

    void Because()
    {
        _parkedSource[0] = ParkedKeyOperation.Delete(_parked.Generation, _parked.Key.Hash);
        _skipSource.Clear();
        _laneSource[0] = _laneSource[0] with { Attempts = 99 };
    }

    [Fact] void should_keep_the_original_f_operation() => _captured.ParkedKeys[0].Kind.ShouldEqual(DeliveryRowOperationKind.Insert);
    [Fact] void should_keep_the_original_s_operation() => _captured.SkipRanges.Length.ShouldEqual(1);
    [Fact] void should_keep_the_original_lane() => _captured.Root.Lanes[0].Attempts.ShouldEqual(0UL);
    [Fact] void should_compare_by_content() => _captured.ShouldEqual(_equal);
    [Fact] void should_hash_by_content() => _captured.GetHashCode().ShouldEqual(_equal.GetHashCode());
    [Fact] void should_remain_valid() => Catch.Exception(() => DeliveryCommitValidator.Validate(_captured)).ShouldBeNull();
}
