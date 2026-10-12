// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_the_revision_does_not_follow_the_expected : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root with { Revision = new(2) }, [], []);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_the_commit() => _exception.ShouldBeOfExactType<InvalidDeliveryCommit>();
}
