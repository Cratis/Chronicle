// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_unwrap_fails_after_erasure : given.a_property_handler
{
    JsonNode _value;
    ReleasedProtectedValue _result;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns(_ => throw new EncryptionKeyUnwrapFailed(new CryptographicException("unwrap failed")));
        _keyStore.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], false));
    }

    async Task Because() => _result = await _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_report_erased() => _result.Status.ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_the_original_ciphertext() => _result.Value.ShouldBeSame(_value);
}
