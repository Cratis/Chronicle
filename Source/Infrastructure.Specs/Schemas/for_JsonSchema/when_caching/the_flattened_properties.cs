// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_flattened_properties : Specification
{
    record Person(string Name, string Email);

    JsonSchema _schema;
    IEnumerable<JsonSchemaProperty> _first;
    IEnumerable<JsonSchemaProperty> _second;

    void Establish() => _schema = JsonSchema.FromType<Person>();

    void Because()
    {
        _first = _schema.GetFlattenedProperties();
        _second = _schema.GetFlattenedProperties();
    }

    [Fact] void should_expose_every_property() => _first.Select(_ => _.Name).ShouldContainOnly(["name", "email"]);
    [Fact] void should_return_the_same_memoized_instance_on_repeated_access() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
