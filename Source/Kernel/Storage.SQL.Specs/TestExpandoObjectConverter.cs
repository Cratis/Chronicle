// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.SQL;

public class TestExpandoObjectConverter : IExpandoObjectConverter
{
    public ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema)
    {
        return new ExpandoObject();
    }

    public JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        return new JsonObject();
    }
}