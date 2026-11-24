// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Dynamic.for_KeyValueEquality.when_comparing_composite_keys.given;

public class equivalent_keys : Specification
{
    protected (object Stored, object Lookup)[] _pairs;

    void Establish()
    {
        var identity = new Identity { Tenant = "tenant", Number = 7 };
        const string json = "{\"tenant\":\"tenant\",\"number\":7}";
        var guid = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109");
        _pairs =
        [
            (identity, new { tenant = "tenant", number = 7L }.AsExpandoObject()),
            (identity, new Dictionary<string, object> { ["tenant"] = "tenant", ["number"] = 7L }),
            (identity, JsonNode.Parse(json)!),
            (identity, JsonSerializer.Deserialize<JsonElement>(json)),
            (identity, json),
            (identity, $"  {json}"),
            (new { key = new Number(7) }, new { KEY = 7L }),
            (new { Key = identity, Parts = new object[] { 7, guid, true, null! } }, "{\"key\":{\"tenant\":\"tenant\",\"number\":7},\"parts\":[7,\"AD037C15-1354-4CD7-A069-F24AA1706109\",true,null]}"),
            (new { id = guid.ToString().ToUpperInvariant() }, new { ID = guid.ToString() }),
            (new { id = guid }, new { ID = guid.ToString().ToUpperInvariant() }),
            (new { numbers = new[] { 7, 8 } }, new { Numbers = new[] { 7L, 8L } })
        ];
    }

    public record Number(int Value) : ConceptAs<int>(Value);

    public class Identity
    {
        public string Tenant { get; init; } = string.Empty;
        public int Number { get; init; }
    }
}
