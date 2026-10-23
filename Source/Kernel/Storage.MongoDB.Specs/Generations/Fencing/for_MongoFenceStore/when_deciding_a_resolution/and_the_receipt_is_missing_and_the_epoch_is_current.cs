// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_MongoFenceStore.when_deciding_a_resolution;

public class and_the_receipt_is_missing_and_the_epoch_is_current : Specification
{
    UnknownCommitResolution _result;

    void Because() => _result = MongoFenceStore.Decide(false, 7, 7);

    [Fact] void should_resolve_as_not_committed() => _result.ShouldEqual(UnknownCommitResolution.NotCommitted);
}
