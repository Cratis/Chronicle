// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Cratis.Chronicle.Dynamic.for_KeyValueEquality.when_comparing_composite_keys.given;

public class unequal_keys : Specification
{
    protected (object Stored, object Lookup)[] _pairs;

    void Establish()
    {
        var identity = new { tenant = "tenant", number = 7 };
        var collision = new Dictionary<string, object> { ["id"] = 7, ["Id"] = 7 };
        var duplicate = JsonSerializer.Deserialize<JsonElement>("{\"id\":7,\"id\":7}");
        var custom = new AmbiguousIdentity();
        var cycle = new CyclicIdentity();
        cycle.Next = cycle;
        _pairs =
        [
            (identity, new { tenant = "tenant" }),
            (identity, new { tenant = "tenant", number = 7, extra = true }),
            (identity, new { tenant = "tenant", number = 8 }),
            (identity, new { tenant = "TENANT", number = 7 }),
            (identity, new { tenant = "tenant", number = "7" }),
            (new { number = 9007199254740992L }, new { number = 9007199254740993L }),
            ("{\"number\":1.00000000000000000000000000001}", "{\"number\":1.00000000000000000000000000002}"),
            (new { values = new[] { 1, 2 } }, new { values = new[] { 2, 1 } }),
            (new { values = new[] { 1 } }, new { values = new[] { 1, 2 } }),
            (new { key = collision }, new { key = new { id = 7 } }),
            (collision, collision),
            (duplicate, duplicate),
            ("{\"id\":7,\"id\":7}", new { id = 7 }),
            ("{\"id\":7,\"id\":7}", "{\"id\":7,\"id\":7}"),
            (identity, "{broken"),
            (custom, custom),
            (custom, new { id = 7 }),
            (identity, identity.GetType().ToString()),
            (cycle, cycle),
            (new { id = "AD037C1513544CD7A069F24AA1706109" }, new { id = "ad037c1513544cd7a069f24aa1706109" })
        ];
    }

    [SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Exercises an intentionally ambiguous composite identity.")]
    public class AmbiguousIdentity
    {
        public int id => 7;
        public int Id => 7;
        public override bool Equals(object? obj) => true;
        public override int GetHashCode() => 0;
    }

    public class CyclicIdentity
    {
        public CyclicIdentity? Next { get; set; }
    }
}
