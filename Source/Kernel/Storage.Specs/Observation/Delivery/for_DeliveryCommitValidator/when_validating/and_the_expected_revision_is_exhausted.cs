// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_the_expected_revision_is_exhausted : given.a_valid_commit
{
    Exception _exception;

    void Establish() => _commit = Create(_root with { Revision = new(long.MaxValue) }, [], []) with { Expected = new(long.MaxValue) };
    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_refuse_to_wrap_the_revision() => _exception.ShouldBeOfExactType<DeliveryRevisionExhausted>();
}
