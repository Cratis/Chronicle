// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_applying_an_empty_value;

public class and_the_subject_was_erased : given.a_property_handler
{
    JsonNode _result;

    void Establish()
    {
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKey?>(null));
        _keyStore.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, Arg.Any<EncryptionKey>())
            .Returns<Task<EncryptionKey>>(_ => throw new InvalidOperationException("The erased key cannot be provisioned."));
    }

    async Task Because() => _result = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(string.Empty));

    [Fact] void should_store_the_empty_value_as_is() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] async Task should_not_provision_a_key() => await _keyStore.DidNotReceive().GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, Arg.Any<EncryptionKey>());
    [Fact] void should_not_encrypt() => _encryption.DidNotReceive().Encrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
