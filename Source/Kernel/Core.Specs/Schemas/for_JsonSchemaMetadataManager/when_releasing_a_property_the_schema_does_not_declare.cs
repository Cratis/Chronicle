// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_releasing_a_property_the_schema_does_not_declare : given.a_value_handler_and_a_type_with_one_property
{
    const string UnknownPropertyName = "propertyTheSchemaNeverHeardOf";
    JsonObject _result;

    void Establish() => _input[UnknownPropertyName] = "some value";

    async Task Because() => _result = await _manager.Release(
        EventStoreName.NotSet,
        EventStoreNamespaceName.Default,
        _schema,
        "request-42",
        _input);

    [Fact] void should_preserve_the_unclassified_member() => _result[UnknownPropertyName]!.GetValue<string>().ShouldEqual("some value");
    [Fact] void should_still_release_the_declared_member() => _result.ContainsKey(PropertyName).ShouldBeTrue();
}
