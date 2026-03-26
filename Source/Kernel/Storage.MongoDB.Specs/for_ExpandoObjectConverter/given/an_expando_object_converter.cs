// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter.given;

public class an_expando_object_converter : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        var typeFormats = new TypeFormats();
        schema = JsonSchema.FromType<TargetType>();
        converter = new(typeFormats);
    }
}
