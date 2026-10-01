// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

/// <summary>
/// Only an erased subject stores the placeholder. Any other failure to protect a read model value still fails,
/// because storing a value that was never protected is not acceptable.
/// </summary>
public class when_applying_to_a_read_model_fails_for_another_reason : given.a_value_handler_and_a_type_with_one_property
{
    Exception _exception;

    void Establish() =>
        _valueHandler
            .Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns<Task<JsonNode>>(_ => throw new CryptographicException("no key"));

    async Task Because() => _exception = await Catch.Exception(() => _manager.ApplyToReadModel(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, "request-42", _input));

    [Fact] void should_fail_with_the_compliance_action_exception() => _exception.ShouldBeOfExactType<SchemaMetadataActionFailed>();
}
