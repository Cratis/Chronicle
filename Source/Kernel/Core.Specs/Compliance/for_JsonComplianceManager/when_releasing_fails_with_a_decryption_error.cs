// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// A release that fails to decrypt means the value was encrypted under a different subject — the classic cause
/// being a projection join copying [PII] out of another event source's stream. The underlying cryptography only
/// reports an opaque padding error, so the diagnostic has to name the property, the subject, and the likely cause.
/// </summary>
public class when_releasing_fails_with_a_decryption_error : given.a_value_handler_and_a_type_with_one_property
{
    const string Identifier = "request-42";

    Exception _exception;

    void Establish() =>
        _valueHandler
            .Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns<Task<JsonNode>>(_ => throw new CryptographicException("error:02000079:rsa routines::oaep decoding error"));

    async Task Because() => _exception = await Catch.Exception(() => _manager.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        Identifier,
        _input));

    [Fact] void should_fail_with_the_compliance_action_exception() => _exception.ShouldBeOfExactType<ComplianceMetadataActionFailed>();

    [Fact] void should_name_the_property() => _exception.Message.ShouldContain(PropertyName);

    [Fact] void should_name_the_subject_it_was_released_under() => _exception.Message.ShouldContain(Identifier);

    [Fact] void should_explain_the_value_belongs_to_another_subject() => _exception.Message.ShouldContain("encrypted under a different subject");

    [Fact] void should_point_at_the_analyzer_that_prevents_it() => _exception.Message.ShouldContain("CHR0038");

    [Fact] void should_keep_the_underlying_error() => _exception.InnerException.ShouldBeOfExactType<CryptographicException>();
}
