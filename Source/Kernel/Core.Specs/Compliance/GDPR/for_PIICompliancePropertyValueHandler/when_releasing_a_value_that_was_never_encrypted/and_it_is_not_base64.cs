// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_a_value_that_was_never_encrypted;

/// <summary>
/// A display name resolved in memory at the query edge is plain text, not base64 — the shape that used to fail
/// the entire enclosing query on a FormatException before it ever reached decryption.
/// </summary>
public class and_it_is_not_base64 : given.a_property_handler
{
    const string Plaintext = "Jane Doe";

    JsonNode _result;

    async Task Because() => _result = await _handler.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        Identifier,
        JsonValue.Create(Plaintext));

    [Fact] void should_pass_the_value_through_untouched() => _result.ToString().ShouldEqual(Plaintext);
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
