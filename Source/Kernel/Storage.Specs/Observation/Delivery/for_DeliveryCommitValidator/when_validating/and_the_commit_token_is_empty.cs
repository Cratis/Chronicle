// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_the_commit_token_is_empty : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root with { LastCommit = new(Guid.Empty) }, [], []);
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_the_unusable_commit_token() => _exception.ShouldBeOfExactType<InvalidDeliveryCommit>();
}
