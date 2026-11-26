// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_the_value_is_released_at_another_address : given.a_signed_value_after_erasure
{
    const string AnotherIdentifier = "another subject";

    void Establish() => _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, AnotherIdentifier).Returns(FenceRecording(_key.Fingerprint));

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, AnotherIdentifier, _value));

    [Fact] void should_fail_closed_instead_of_returning_erased() => _error.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
}
