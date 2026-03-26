// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter.given;

public class an_expando_object_converter_with_nested_children_schema : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        var typeFormats = new TypeFormats();
        schema = JsonSchema.FromType<ParentWithNestedChildren>();
        converter = new(typeFormats);
    }
}
