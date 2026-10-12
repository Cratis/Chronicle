// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_skip_is_recorded_while_its_parked_key_is_deleted : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root, [ParkedKeyOperation.Delete(_parked.Generation, _parked.Key.Hash)], [SkipRangeOperation.Insert(_skip)]);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_accept_separate_f_and_s_namespaces() => _exception.ShouldBeNull();
}
