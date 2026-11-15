// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_reporting_erasure;

public class when_using_the_existing_release : given.a_property_handler
{
    JsonNode _result;

    void Establish()
    {
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(Convert.ToBase64String(new byte[256])));

    [Fact] void should_keep_the_existing_empty_value() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_add_erasure_lookups_to_ordinary_reads() => _keyStore.DidNotReceive().GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>());
}
