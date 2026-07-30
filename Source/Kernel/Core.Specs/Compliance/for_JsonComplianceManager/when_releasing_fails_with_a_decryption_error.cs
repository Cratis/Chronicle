// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

/// <summary>
/// A release that fails to decrypt means the value was encrypted under a different subject — the classic cause
/// being a projection join copying [PII] out of another event source's stream. That is a real modeling defect,
/// but it belongs to a single property: failing the release would take down every other property in the result
/// and the query returning it. The property is surfaced as empty — the shape an erased subject already
/// produces — and the diagnostic naming the property, the subject and the likely cause goes to the log.
/// </summary>
public class when_releasing_fails_with_a_decryption_error : given.a_value_handler_and_a_type_with_one_property
{
    const string Identifier = "request-42";

    Exception _exception;
    JsonObject _result;

    void Establish() =>
        _valueHandler
            .Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns<Task<JsonNode>>(_ => throw new CryptographicException("error:02000079:rsa routines::oaep decoding error"));

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _manager.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        Identifier,
        _input));

    [Fact] void should_not_fail_the_release() => _exception.ShouldBeNull();

    [Fact] void should_surface_the_unreadable_property_as_empty() => _result[PropertyName].ToString().ShouldEqual(string.Empty);
}
