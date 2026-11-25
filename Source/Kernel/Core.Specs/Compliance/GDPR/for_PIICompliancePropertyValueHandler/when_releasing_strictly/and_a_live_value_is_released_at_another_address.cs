// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_a_live_value_is_released_at_another_address : given.a_signed_value_after_erasure
{
    const string AnotherIdentifier = "another subject";

    void Establish()
    {
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, AnotherIdentifier).Returns(new EncryptionKeyErasure(0, [], true));
        _keyStore.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, AnotherIdentifier, new EncryptionKeyRevision(2)).Returns(new EncryptionKeyAtRevision(_key, 2));
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, AnotherIdentifier, _value));

    [Fact] void should_fail_closed_even_though_the_key_is_live() => _error.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
}
