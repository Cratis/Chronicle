// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison.when_comparing_materializations.given;

public class unequal_keys : Specification
{
    protected (object Stored, object Lookup)[] _pairs;

    void Establish()
    {
        var key = new { tenant = "tenant", number = 7 };
        var collision = new Dictionary<string, object> { ["id"] = 7, ["Id"] = 7 };
        var duplicate = JsonSerializer.Deserialize<JsonElement>("{\"id\":7,\"id\":7}");
        const string malformed = "{broken";
        _pairs =
        [
            (key, new { tenant = "tenant" }),
            (key, new { tenant = "tenant", number = 7, extra = true }),
            (key, new { tenant = "tenant", number = 8 }),
            (key, new { tenant = "TENANT", number = 7 }),
            (key, new { tenant = "tenant", number = "7" }),
            (new { number = 9007199254740992L }, new { number = 9007199254740993L }),
            ("{\"number\":1.00000000000000000000000000001}", "{\"number\":1.00000000000000000000000000002}"),
            (new { values = new[] { 1, 2 } }, new { values = new[] { 2, 1 } }),
            (new { key = collision }, new { key = new { id = 7 } }),
            (collision, collision),
            (duplicate, duplicate),
            ("{\"id\":7,\"id\":7}", new { id = 7 }),
            ("{\"id\":7,\"id\":7}", "{\"id\":7,\"id\":7}"),
            (key, malformed),
            (key, key.GetType().ToString()),
            (new { id = "AD037C1513544CD7A069F24AA1706109" }, new { id = "ad037c1513544cd7a069f24aa1706109" })
        ];
    }
}
