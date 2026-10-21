// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

public abstract class a_security_schema : Specification
{
    protected const string Ciphertext = "opaque-ciphertext";
    protected ExpandoObjectConverter _converter;
    protected JsonSchema _schema;
    protected JsonObject _protectedContent;
    protected JsonObject _roundTripped;
    protected abstract string MetadataType { get; }

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "count": { "type": "integer", "security": [{"metadataType":"SCOPE","details":""}] },
                    "formattedCount": { "type": "integer", "format":"int32", "security": [{"metadataType":"SCOPE","details":""}] },
                    "flag": { "type": "boolean", "security": [{"metadataType":"SCOPE","details":""}] },
                    "rate": { "type": "number", "security": [{"metadataType":"SCOPE","details":""}] },
                    "amount": { "type": "number", "format":"decimal", "security": [{"metadataType":"SCOPE","details":""}] },
                    "object": { "type": "object", "properties":{"count":{"type":"integer"}}, "security": [{"metadataType":"SCOPE","details":""}] },
                    "array": { "type": "array", "items":{"type":"boolean"}, "security": [{"metadataType":"SCOPE","details":""}] },
                    "nested": { "type":"object", "properties":{"flag":{"type":"boolean","security":[{"metadataType":"SCOPE","details":""}]}} },
                    "items": { "type":"array", "items":{"type":"integer","security":[{"metadataType":"SCOPE","details":""}]}}
                }
            }
            """.Replace("SCOPE", MetadataType));
        _protectedContent = new JsonObject
        {
            ["count"] = Ciphertext,
            ["formattedCount"] = Ciphertext,
            ["flag"] = Ciphertext,
            ["rate"] = Ciphertext,
            ["amount"] = Ciphertext,
            ["object"] = Ciphertext,
            ["array"] = Ciphertext,
            ["nested"] = new JsonObject { ["flag"] = Ciphertext },
            ["items"] = new JsonArray(Ciphertext)
        };
    }
}
