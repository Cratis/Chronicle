// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_the_key_is_live : given.a_signed_value_after_erasure
{
    JsonNode _result;

    void Establish()
    {
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(0, [], true));
        _keyStore.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new EncryptionKeyRevision(2)).Returns(new EncryptionKeyAtRevision(_key, 2));
    }

    async Task Because() => _result = await _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_release_the_value() => _result.ToString().ShouldEqual("value");
}
