// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_two_parked_keys_share_a_ready_ticket : given.a_valid_commit
{
    Exception _exception;

    void Establish()
    {
        var other = _parked with { Key = SequencingKeyIdentity.For(new ValueSequencingKey(["other"])) };
        _commit = Create(_root, [ParkedKeyOperation.Insert(_parked), ParkedKeyOperation.Upsert(other)], []);
    }

    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_reject_shared_ready_tickets() => _exception.ShouldBeOfExactType<InvalidDeliveryCommit>();
}
