// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// Encryption failing on the way in is a different problem from a subject mismatch on the way out, so the
/// subject hint must not be attached to it.
/// </summary>
public class when_applying_fails : given.a_value_handler_and_a_type_with_one_property
{
    const string Identifier = "request-42";

    Exception _exception;

    void Establish() =>
        _valueHandler
            .Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns<Task<JsonNode>>(_ => throw new CryptographicException("no key"));

    async Task Because() => _exception = await Catch.Exception(() => _manager.Apply(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        Identifier,
        _input));

    [Fact] void should_fail_with_the_compliance_action_exception() => _exception.ShouldBeOfExactType<ComplianceMetadataActionFailed>();

    [Fact] void should_name_the_property() => _exception.Message.ShouldContain(PropertyName);

    [Fact] void should_not_suggest_a_subject_mismatch() => _exception.Message.ShouldNotContain("encrypted under a different subject");
}
