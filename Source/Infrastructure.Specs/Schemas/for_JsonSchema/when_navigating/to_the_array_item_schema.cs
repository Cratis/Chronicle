// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_navigating;

public class to_the_array_item_schema : Specification
{
    record Tag(string Name);
    record Post(string Title, IEnumerable<Tag> Tags);

    JsonSchema _schema;

    void Because() => _schema = JsonSchema.FromType<Post>();

    [Fact] void should_recognize_the_collection_property_as_an_array() => _schema.ActualProperties["tags"].IsArray.ShouldBeTrue();
    [Fact] void should_expose_the_item_schema() => _schema.ActualProperties["tags"].Item.ShouldNotBeNull();
    [Fact] void should_expose_the_item_properties() => _schema.ActualProperties["tags"].Item.ActualProperties.Keys.ShouldContain("name");
}
