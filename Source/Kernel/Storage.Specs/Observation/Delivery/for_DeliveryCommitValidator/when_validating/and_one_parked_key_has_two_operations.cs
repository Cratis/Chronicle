// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_one_parked_key_has_two_operations : given.a_valid_commit
{
    [Theory]
    [InlineData(DeliveryRowOperationKind.Insert)]
    [InlineData(DeliveryRowOperationKind.Upsert)]
    [InlineData(DeliveryRowOperationKind.Delete)]
    void should_reject_duplicate_identities(DeliveryRowOperationKind kind)
    {
        var second = kind switch
        {
            DeliveryRowOperationKind.Insert => ParkedKeyOperation.Insert(_parked),
            DeliveryRowOperationKind.Upsert => ParkedKeyOperation.Upsert(_parked),
            _ => ParkedKeyOperation.Delete(_parked.Generation, _parked.Key.Hash)
        };
        var commit = Create(_root, [ParkedKeyOperation.Insert(_parked), second], []);
        Catch.Exception(() => DeliveryCommitValidator.Validate(commit)).ShouldBeOfExactType<InvalidDeliveryCommit>();
    }
}
