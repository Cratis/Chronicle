// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

/// <summary>
/// A schema shaped the way a read model's registered schema actually is - a nullable enum carrying its member
/// list and no format suffix, beside a nullable flag, a non-nullable enum and a non-nullable number.
/// </summary>
public class an_expando_object_converter_with_a_read_model_schema : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "id": { "type": "string" },
                    "rejectionReason": {
                        "type": ["integer", "null"],
                        "enum": [1, 2, 3, 4],
                        "x-enumNames": ["RejectedBySigner", "Canceled", "Expired", "Failed"]
                    },
                    "status": {
                        "type": "integer",
                        "enum": [1, 2, 3],
                        "x-enumNames": ["Draft", "Signed", "Terminated"]
                    },
                    "isFinalized": { "type": ["boolean", "null"] },
                    "rate": { "type": "number", "format": "decimal" }
                }
            }
            """);
        converter = new(new TypeFormats());
    }
}
