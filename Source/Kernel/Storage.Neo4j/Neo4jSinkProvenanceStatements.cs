// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Defines queries and row decoding for the database-local provenance metadata.
/// </summary>
internal static class Neo4jSinkProvenanceStatements
{
    /// <summary>Creates the uniqueness constraint on metadata keys.</summary>
    internal const string CreateConstraint = "CREATE CONSTRAINT cratis_sink_inventory_key IF NOT EXISTS FOR " + Pattern + " REQUIRE n._key IS UNIQUE";

    /// <summary>Finds the uniqueness constraint on metadata keys.</summary>
    internal const string VerifyConstraint = "SHOW CONSTRAINTS YIELD type, entityType, labelsOrTypes, properties " +
        "WHERE type IN ['UNIQUENESS', 'NODE_PROPERTY_UNIQUENESS'] AND entityType = 'NODE' AND labelsOrTypes = ['" + Label + "'] AND properties = ['_key'] RETURN type";

    /// <summary>Reads all provenance metadata records.</summary>
    internal const string Read = "MATCH " + Pattern + " RETURN properties(n) AS state";

    /// <summary>Creates an incomplete inventory record if none exists.</summary>
    internal const string Seed = "MERGE " + Inventory + " ON CREATE SET " +
        "n._format = 1, n._id = $id, n._version = 0, n._restricted = false";

    /// <summary>Acquires the inventory record's write lock for the transaction.</summary>
    internal const string LockInventory = "MATCH " + Inventory + " SET n._lock = true REMOVE n._lock";

    /// <summary>Records inventory completion and restriction evidence.</summary>
    internal const string Complete = "MATCH " + Inventory + " SET n._version = $version, n._restricted = $restricted";

    /// <summary>Persists the separate protection record.</summary>
    internal const string Mark = "MERGE " + Protection + " ON CREATE SET n._format = 1";

    /// <summary>Persists the restriction on the inventory record.</summary>
    internal const string LatchRestriction = "MATCH " + Inventory + " SET n._restricted = true";

    const string Label = GraphBookkeeping.ProvenanceLabel;
    const string Pattern = "(n:" + Label + ")";
    const string Inventory = "(n:" + Label + " {_key: 'inventory'})";
    const string Protection = "(n:" + Label + " {_key: 'protected'})";

    /// <summary>
    /// Gets the fields of a supported metadata record.
    /// </summary>
    /// <param name="record">The record returned by the database.</param>
    /// <returns>The metadata fields.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the record is invalid or unsupported.</exception>
    internal static IReadOnlyDictionary<string, object> Fields(IRecord record) =>
        record["state"] is IReadOnlyDictionary<string, object> fields && Get<long>(fields, "_format") == 1
            ? fields
            : throw Neo4jGraphTarget.Unavailable();

    /// <summary>
    /// Decodes and validates an inventory record.
    /// </summary>
    /// <param name="fields">The inventory fields.</param>
    /// <returns>The validated inventory state.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the inventory fields are invalid.</exception>
    internal static SinkProvenanceState ParseInventory(IReadOnlyDictionary<string, object> fields)
    {
        if (fields.Count != 5 || !Guid.TryParseExact(Get<string>(fields, "_id"), "D", out var id)) throw Neo4jGraphTarget.Unavailable();
        var version = Get<long>(fields, "_version");
        if (version is not (0 or SinkProvenanceState.CurrentInventoryVersion)) throw Neo4jGraphTarget.Unavailable();
        return new SinkProvenanceState(new(id), (int)version, Get<bool>(fields, "_restricted")).Validate();
    }

    /// <summary>
    /// Reads a required metadata field of the expected type.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="fields">The metadata fields.</param>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the field is missing or has the wrong type.</exception>
    internal static T Get<T>(IReadOnlyDictionary<string, object> fields, string name) =>
        fields.TryGetValue(name, out var value) && value is T actual ? actual : throw Neo4jGraphTarget.Unavailable();
}
