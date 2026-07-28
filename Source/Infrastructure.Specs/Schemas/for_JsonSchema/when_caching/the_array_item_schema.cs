// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_array_item_schema : Specification
{
    record Tag(string Name);
    record Post(string Title, IEnumerable<Tag> Tags);

    JsonSchemaProperty _property;
    JsonSchema? _first;
    JsonSchema? _second;

    void Establish() => _property = JsonSchema.FromType<Post>().ActualProperties["tags"];

    void Because()
    {
        _first = _property.Item;
        _second = _property.Item;
    }

    [Fact] void should_expose_the_item_schema() => _first.ShouldNotBeNull();
    [Fact] void should_return_the_same_instance_on_repeated_access() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
