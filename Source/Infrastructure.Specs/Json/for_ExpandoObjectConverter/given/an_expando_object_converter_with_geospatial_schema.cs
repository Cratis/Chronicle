// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

public class an_expando_object_converter_with_geospatial_schema : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        var typeFormats = new TypeFormats();
        schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "pointValue": { "type": "object", "format": "point" },
                    "lineStringValue": { "type": "object", "format": "linestring" },
                    "polygonValue": { "type": "object", "format": "polygon" }
                }
            }
            """);
        converter = new(typeFormats);
    }
}
