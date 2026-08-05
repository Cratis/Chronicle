// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_a_value_that_was_never_encrypted;

/// <summary>
/// A key is only ever minted for a subject that encrypts something at rest, so a read model keyed by a hash, a
/// cluster identifier or any other computed identity has none - and a name resolved in memory at the query edge
/// for display on such a read model came back as an empty string. That is the same answer an erased subject
/// gets, which is why it goes unnoticed: no error, no log, no failed query, just a blank where correct plaintext
/// was.
/// </summary>
/// <remarks>
/// Showing a person's name on a view whose own compliance subject is an aggregate is ordinary, and the
/// alternative - denormalizing the name into storage under the aggregate's subject - is worse for compliance,
/// because it puts erasable personal data outside the person's own erasure scope.
/// <para>
/// Whether the subject holds a key cannot decide this on its own. It answers "can this be decrypted", and the
/// question here is "was this ever encrypted" - which the value's own shape answers, for a shredded value as
/// well as for a plaintext one.
/// </para>
/// </remarks>
public class and_the_subject_has_no_key : given.a_property_handler
{
    const string Plaintext = "Jane Doe";

    JsonNode _result;

    void Establish() => _keyStore
        .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier)
        .Returns(Task.FromResult<EncryptionKey?>(null));

    async Task Because() => _result = await _handler.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        Identifier,
        JsonValue.Create(Plaintext));

    [Fact] void should_pass_the_value_through_untouched() => _result.ToString().ShouldEqual(Plaintext);
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
