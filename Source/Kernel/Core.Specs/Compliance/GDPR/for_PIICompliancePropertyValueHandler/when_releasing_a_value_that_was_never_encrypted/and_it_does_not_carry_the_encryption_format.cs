// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_a_value_that_was_never_encrypted;

/// <summary>
/// The value decodes as base64 but carries none of the shape encryption produces, so it was never encrypted
/// under this subject. Releasing it is a no-op — the subject owning a key says nothing about this value.
/// </summary>
public class and_it_does_not_carry_the_encryption_format : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;

    void Establish()
    {
        _input = JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes("Jane Doe")));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(false);
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_pass_the_value_through_untouched() => _result.ToString().ShouldEqual(_input.ToString());
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
