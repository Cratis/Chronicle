// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_the_fingerprint_is_not_recorded_and_the_key_is_missing : given.a_signed_value_after_erasure
{
    void Establish() => _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(FenceRecording("another fingerprint"));

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_not_treat_the_value_as_erased() => _error.ShouldBeOfExactType<MissingEncryptionKey>();
}
