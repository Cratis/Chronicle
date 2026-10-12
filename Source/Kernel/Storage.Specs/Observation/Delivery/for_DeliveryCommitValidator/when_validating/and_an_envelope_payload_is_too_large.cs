// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_an_envelope_payload_is_too_large : given.a_valid_commit
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_reject_either_reserved_payload(bool coverage)
    {
        var envelope = new VersionedEnvelope(0, new byte[65537].AsSpan());
        var root = coverage ? _root with { CoverageHead = envelope } : _root with { Operation = envelope };
        Catch.Exception(() => DeliveryCommitValidator.Validate(Create(root, [], []))).ShouldBeOfExactType<DeliveryValueOutOfRange>();
    }
}
