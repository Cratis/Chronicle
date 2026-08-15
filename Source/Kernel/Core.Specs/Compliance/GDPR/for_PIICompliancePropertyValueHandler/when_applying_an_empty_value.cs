// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

/// <summary>
/// Rebuilding a read model re-applies whatever the release produced, and every PII value for an erased subject
/// releases as empty - so an erased subject's rebuild arrives here with nothing in it.
/// </summary>
/// <remarks>
/// Provisioning a key for that would be resurrection by replay; refusing to would break the rebuild over data that
/// is already gone. An empty value holds no personal data, so neither is needed: it passes straight through, and
/// the release path passes it back unchanged, which leaves the round trip exactly where it was.
/// </remarks>
public class when_applying_an_empty_value : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;

    void Establish() => _input = JsonValue.Create(string.Empty);

    async Task Because() => _result = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_return_the_value_untouched() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_encrypt_anything() => _encryption.DidNotReceive().Encrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
    [Fact] void should_not_provision_a_key() => _keyStore.DidNotReceive().GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>());
}
