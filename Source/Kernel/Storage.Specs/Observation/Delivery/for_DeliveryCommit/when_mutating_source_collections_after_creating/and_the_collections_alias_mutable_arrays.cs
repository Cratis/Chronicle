// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommit.when_mutating_source_collections_after_creating;

public class and_the_collections_alias_mutable_arrays : for_DeliveryCommitValidator.given.a_valid_commit
{
    ParkedKeyOperation[] _parkedSource;
    SkipRangeOperation[] _skipSource;
    DeliveryCommit _captured;

    void Establish()
    {
        _parkedSource = [ParkedKeyOperation.Insert(_parked)];
        _skipSource = [SkipRangeOperation.Insert(_skip)];
        _captured = new(DeliveryRevision.None, _root, ImmutableCollectionsMarshal.AsImmutableArray(_parkedSource), ImmutableCollectionsMarshal.AsImmutableArray(_skipSource));
    }

    void Because()
    {
        _parkedSource[0] = ParkedKeyOperation.Delete(_parked.Generation, _parked.Key.Hash);
        _skipSource[0] = SkipRangeOperation.Delete(_skip.Generation, _skip.Key.Hash, _skip.From);
    }

    [Fact] void should_copy_the_f_array_at_construction() => _captured.ParkedKeys[0].Kind.ShouldEqual(DeliveryRowOperationKind.Insert);
    [Fact] void should_copy_the_s_array_at_construction() => _captured.SkipRanges[0].Kind.ShouldEqual(DeliveryRowOperationKind.Insert);
}
