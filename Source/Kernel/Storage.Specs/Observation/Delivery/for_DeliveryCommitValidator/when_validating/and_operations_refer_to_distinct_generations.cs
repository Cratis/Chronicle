// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_operations_refer_to_distinct_generations : given.a_valid_commit
{
    Exception _exception;

    void Establish()
    {
        _commit = Create(_root,
            [ParkedKeyOperation.Upsert(_parked), ParkedKeyOperation.Delete(new(1), _parked.Key.Hash)],
            [SkipRangeOperation.Insert(_skip), SkipRangeOperation.Insert(_skip with { Generation = new(1) }), SkipRangeOperation.Delete(new(2), _skip.Key.Hash, _skip.From)]);
    }

    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_keep_generation_scoped_row_namespaces_separate() => _exception.ShouldBeNull();
}
