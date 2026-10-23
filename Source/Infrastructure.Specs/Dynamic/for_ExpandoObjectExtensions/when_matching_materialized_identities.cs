// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_matching_materialized_identities : Specification
{
    readonly List<bool> _matches = [];
    readonly List<bool> _nonMatches = [];

    void Because()
    {
        var id = Guid.NewGuid();
        var identity = new CompositeIdentity("tenant", 42);
        (object Stored, object Lookup)[] matches =
        [
            (42, "42"),
            (42L, 42),
            (id, id.ToString()),
            (id.ToString(), id),
            (identity, new CompositeIdentity("tenant", 42)),
            (identity.AsExpandoObject(), identity),
            (identity, identity.AsExpandoObject()),
            (identity.AsExpandoObject(), new Dictionary<string, object> { ["Number"] = 42, ["Tenant"] = "tenant" }),
            (new Dictionary<string, object> { ["Number"] = 42, ["Tenant"] = "tenant" }, identity.AsExpandoObject()),
            (identity.AsExpandoObject(), "{\"Number\":42,\"Tenant\":\"tenant\"}"),
            ("{\"Number\":42,\"Tenant\":\"tenant\"}", identity.AsExpandoObject()),
            (new { nested = identity }.AsExpandoObject(), new { nested = new CompositeIdentity("tenant", 42) }.AsExpandoObject())
        ];
        (object Stored, object Lookup)[] nonMatches =
        [
            (42, "not-a-number"),
            (42, "9999999999999999999999999"),
            (id, "not-a-guid"),
            (42, id),
            (42, id.ToString()),
            (Role.Administrator, id),
            (Role.Administrator, id.ToString()),
            (identity.AsExpandoObject(), new CompositeIdentity("tenant", 43).AsExpandoObject()),
            (identity.AsExpandoObject(), new { Tenant = "tenant" }.AsExpandoObject()),
            (new { Tenant = "tenant" }.AsExpandoObject(), identity.AsExpandoObject()),
            (identity.AsExpandoObject(), "{invalid-json"),
            (identity.AsExpandoObject(), new Dictionary<string, object> { ["Number"] = 43, ["Tenant"] = "tenant" })
        ];
        foreach (var (stored, lookup) in matches)
        {
            CheckLookup(stored, lookup, _matches);
        }
        foreach (var (stored, lookup) in nonMatches)
        {
            CheckLookup(stored, lookup, _nonMatches);
        }
    }

    [Fact] void should_match_equivalent_scalar_and_composite_values() => _matches.ShouldEachConformTo(match => match);
    [Fact] void should_reject_incompatible_or_different_values() => _nonMatches.ShouldEachConformTo(match => !match);

    static void CheckLookup(object stored, object lookup, List<bool> results)
    {
        dynamic child = new ExpandoObject();
        child.id = stored;
        ExpandoObject[] children = [child];
        results.Add(children.Contains(new PropertyPath("id"), lookup));
        results.Add(ReferenceEquals(children.GetItem(new PropertyPath("id"), lookup), child));
    }

    record CompositeIdentity(string Tenant, int Number);

    enum Role
    {
        Reader = 0,
        Administrator = 1
    }
}
