// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_the_key_is_missing_without_erasure : given.a_property_handler
{
    JsonNode _value;
    Exception _error;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKey?>(null));
    }

    async Task Because() => _error = (await Catch.Exception(() => _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value)))!;

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<MissingEncryptionKey>();
}
