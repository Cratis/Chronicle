// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_reporting_erasure;

public class with_a_live_key_and_unwrap_failure : given.a_property_handler
{
    Exception _error;

    void Establish()
    {
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Throws(new EncryptionKeyUnwrapFailed(new CryptographicException()));
        _keyStore.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], true));
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseReportingErasure(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(Convert.ToBase64String(new byte[256]))));

    [Fact] void should_preserve_the_release_failure() => _error.ShouldBeOfExactType<EncryptionKeyUnwrapFailed>();
    [Fact] void should_not_consult_erasure_for_a_live_key() => _keyStore.DidNotReceive().GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
