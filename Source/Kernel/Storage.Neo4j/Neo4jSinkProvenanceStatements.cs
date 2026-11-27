// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

internal static class Neo4jSinkProvenanceStatements
{
    const string Label = GraphBookkeeping.ProvenanceLabel;
    const string Pattern = "(n:" + Label + ")";
    const string Inventory = "(n:" + Label + " {_key: 'inventory'})";
    const string Protection = "(n:" + Label + " {_key: 'protected'})";

    internal const string CreateConstraint = "CREATE CONSTRAINT cratis_sink_inventory_key IF NOT EXISTS FOR " + Pattern + " REQUIRE n._key IS UNIQUE";
    internal const string VerifyConstraint = "SHOW CONSTRAINTS YIELD type, entityType, labelsOrTypes, properties " +
        "WHERE type IN ['UNIQUENESS', 'NODE_PROPERTY_UNIQUENESS'] AND entityType = 'NODE' AND labelsOrTypes = ['" + Label + "'] AND properties = ['_key'] RETURN type";
    internal const string Read = "MATCH " + Pattern + " RETURN properties(n) AS state";
    internal const string Seed = "MERGE " + Inventory + " ON CREATE SET " +
        "n._format = 1, n._id = $id, n._version = 0, n._restricted = false";
    internal const string Lock = "MATCH " + Inventory + " SET n._lock = true REMOVE n._lock";
    internal const string Complete = "MATCH " + Inventory + " SET n._version = $version, n._restricted = $restricted";
    internal const string Mark = "MERGE " + Protection + " ON CREATE SET n._format = 1";
    internal const string LatchRestriction = "MATCH " + Inventory + " SET n._restricted = true";

    internal static IReadOnlyDictionary<string, object> Fields(IRecord record) =>
        record["state"] is IReadOnlyDictionary<string, object> fields && Get<long>(fields, "_format") == 1
            ? fields
            : throw Neo4jGraphTarget.Unavailable();

    internal static SinkProvenanceState ParseInventory(IReadOnlyDictionary<string, object> fields)
    {
        if (fields.Count != 5 || !Guid.TryParseExact(Get<string>(fields, "_id"), "D", out var id)) throw Neo4jGraphTarget.Unavailable();
        var version = Get<long>(fields, "_version");
        if (version is not (0 or SinkProvenanceState.CurrentInventoryVersion)) throw Neo4jGraphTarget.Unavailable();
        return new SinkProvenanceState(new(id), (int)version, Get<bool>(fields, "_restricted")).Validate();
    }

    internal static T Get<T>(IReadOnlyDictionary<string, object> fields, string name) =>
        fields.TryGetValue(name, out var value) && value is T actual ? actual : throw Neo4jGraphTarget.Unavailable();
}
