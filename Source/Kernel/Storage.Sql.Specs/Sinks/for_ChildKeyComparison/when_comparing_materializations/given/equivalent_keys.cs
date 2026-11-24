// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison.when_comparing_materializations.given;

public class equivalent_keys : Specification
{
    protected (object Stored, object Lookup)[] _pairs;

    void Establish()
    {
        var key = new Identity("tenant", 7);
        const string json = "{\"tenant\":\"tenant\",\"number\":7}";
        _pairs =
        [
            (key, new { tenant = "tenant", number = 7L }.AsExpandoObject()),
            (key, new Dictionary<string, object> { ["tenant"] = "tenant", ["number"] = 7L }),
            (key, JsonNode.Parse(json)!),
            (key, JsonSerializer.Deserialize<JsonElement>(json)),
            (key, json),
            (key, $"  {json}"),
            (new { key, values = new object?[] { 7, true, null } }, "{\"key\":{\"tenant\":\"tenant\",\"number\":7},\"values\":[7,true,null]}"),
            (new { id = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109") }, new { ID = "AD037C15-1354-4CD7-A069-F24AA1706109" }),
            (new { id = "ad037c15-1354-4cd7-a069-f24aa1706109" }, new { ID = "AD037C15-1354-4CD7-A069-F24AA1706109" })
        ];
    }

    public record Identity(string Tenant, int Number);
}
