// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_a_recorded_erased_value_is_corrupted : given.a_signed_value_after_erasure
{
    void Establish()
    {
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(FenceRecording(_key.Fingerprint));
        _value = WithFlippedLastByte(_value);
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_fail_closed_instead_of_returning_empty() => _error.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
}
