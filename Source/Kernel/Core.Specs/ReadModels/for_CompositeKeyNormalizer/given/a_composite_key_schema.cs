// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_CompositeKeyNormalizer.given;

public class a_composite_key_schema : Specification
{
    protected JsonSchema _schema;
    protected ITypeFormats _typeFormats;

    void Establish()
    {
        _typeFormats = new TypeFormats();
        _schema = JsonSchema.FromJson("""
            {
              "type": "object",
              "properties": {
                "id": {
                  "type": "object",
                  "properties": {
                    "currency": { "type": "string" },
                    "invoice": { "type": "integer", "format": "int64" },
                    "reference": { "type": "string", "format": "guid" },
                    "issued": { "type": "string", "format": "date-time-offset" },
                    "amount": { "type": "number", "format": "decimal" },
                    "paid": { "type": "boolean" }
                  }
                },
                "total": { "type": "number", "format": "decimal" }
              }
            }
            """);
    }

    protected static Dictionary<string, string> ValidParts() => new()
    {
        ["currency"] = "\"EUR\"",
        ["invoice"] = "42",
        ["reference"] = "\"0D0B9D4C-6E0A-4C4F-8C1B-2B6E3F6B1A11\"",
        ["issued"] = "\"2026-10-23T10:00:00+02:00\"",
        ["amount"] = "1.50",
        ["paid"] = "true"
    };
}
