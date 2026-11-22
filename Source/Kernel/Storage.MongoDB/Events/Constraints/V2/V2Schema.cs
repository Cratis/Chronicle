// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>Verifies complete feature ownership and physical shape without adopting or repairing storage.</summary>
/// <param name="database">The explicitly selected namespace database.</param>
internal sealed partial class V2Schema(IMongoDatabase database)
{
    /// <summary>The feature-owned authority collection, disjoint from v1.</summary>
    public const string Authorities = "uc+authorities+uc2";

    /// <summary>The feature-owned binding collection, disjoint from v1.</summary>
    public const string Bindings = "uc+bindings+uc2";

    /// <summary>The explicit binary collation for schema and every claim predicate.</summary>
    public static readonly Collation Simple = new("simple");

    static readonly BsonDocument[] _metadataIndexes = [Index("_id_", new("_id", 1))];
    static readonly BsonDocument[] _bindingIndexes = [.. _metadataIndexes, Index("physical", new("Physical", 1), true)];
    static readonly BsonDocument[] _claimIndexes =
    [
        .. _metadataIndexes,
        Index("value", new() { { "Scope", 1 }, { "Value", 1 } }, true),
        Index("owner", new() { { "Scope", 1 }, { "EventSourceId", 1 } }),
        Index("subject", new("Subject", 1))
    ];

    /// <summary>Checks both metadata collections as one coherent feature-owned set.</summary>
    /// <param name="create">Whether explicit authority initialization may bootstrap absent metadata.</param>
    /// <param name="cancellationToken">Cancellation of catalog and marker reads.</param>
    /// <returns>Whether metadata exists.</returns>
    /// <exception cref="UniqueConstraintV2SchemaMismatch">Existing metadata is partial, unrecognized or incompatible.</exception>
    public async Task<bool> EnsureMetadata(bool create, CancellationToken cancellationToken)
    {
        var authorities = await Describe(Authorities, cancellationToken);
        var bindings = await Describe(Bindings, cancellationToken);
        if (authorities is null && bindings is null)
        {
            if (!create) return false;
            await Bootstrap(cancellationToken);
            return await EnsureMetadata(false, cancellationToken);
        }

        if (authorities is null || bindings is null) throw new UniqueConstraintV2SchemaMismatch();
        await Verify(Authorities, authorities, _metadataIndexes, cancellationToken);
        await Verify(Bindings, bindings, _bindingIndexes, cancellationToken);
        await VerifyMarker(Authorities, cancellationToken);
        await VerifyMarker(Bindings, cancellationToken);
        return true;
    }

    /// <summary>Resolves the complete binding, optionally publishing brand-new storage atomically.</summary>
    /// <param name="binding">The complete expected ownership.</param>
    /// <param name="create">Whether Save may create absent claims.</param>
    /// <param name="cancellationToken">Cancellation of resolution.</param>
    /// <returns>The exact collection name, or null for an absent binding.</returns>
    /// <exception cref="UniqueConstraintV2SchemaMismatch">A binding or physical collection is damaged or unowned.</exception>
    public async Task<string?> Resolve(V2Binding binding, bool create, CancellationToken cancellationToken)
    {
        if (!await EnsureMetadata(false, cancellationToken)) return null;
        var metadata = database.GetCollection<BsonDocument>(Bindings);
        var stored = await metadata.Find(new BsonDocument("_id", binding.Id), new FindOptions { Collation = Simple }).SingleOrDefaultAsync(cancellationToken);
        if (stored is not null) return await VerifyBinding(binding, stored, cancellationToken);
        await RequireUnowned(binding, cancellationToken);
        if (!create) return null;
        await Provision(binding, cancellationToken);
        return await Resolve(binding, false, cancellationToken) ?? throw new UniqueConstraintV2SchemaMismatch();
    }

    async Task<string> VerifyBinding(V2Binding binding, BsonDocument stored, CancellationToken cancellationToken)
    {
        if (!stored.Equals(binding.Document)) throw new UniqueConstraintV2SchemaMismatch();
        var description = await Describe(binding.Physical, cancellationToken) ?? throw new UniqueConstraintV2SchemaMismatch();
        await Verify(binding.Physical, description, _claimIndexes, cancellationToken);
        return binding.Physical;
    }

    async Task RequireUnowned(V2Binding binding, CancellationToken cancellationToken)
    {
        if (await Describe(binding.Physical, cancellationToken) is not null) throw new UniqueConstraintV2SchemaMismatch();
        var metadata = database.GetCollection<BsonDocument>(Bindings);
        var physicalOwner = await metadata.Find(new BsonDocument("Physical", binding.Physical), new FindOptions { Collation = Simple }).AnyAsync(cancellationToken);
        if (physicalOwner) throw new UniqueConstraintV2SchemaMismatch();
    }

    static BsonDocument Index(string name, BsonDocument key, bool unique = false) => new() { { "name", name }, { "key", key }, { "unique", unique } };

    static bool IsSimple(BsonDocument options) => !options.TryGetValue("collation", out var collation) || collation is BsonDocument document && document.Equals(new BsonDocument("locale", "simple"));

    async Task<BsonDocument?> Describe(string name, CancellationToken cancellationToken)
    {
        using var cursor = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", name) }, cancellationToken);
        return (await cursor.ToListAsync(cancellationToken)).SingleOrDefault();
    }

    async Task VerifyMarker(string name, CancellationToken cancellationToken)
    {
        var marker = await database.GetCollection<BsonDocument>(name).Find(new BsonDocument("_id", "schema")).SingleOrDefaultAsync(cancellationToken);
        if (marker is null || !marker.Equals(V2Documents.Metadata(name))) throw new UniqueConstraintV2SchemaMismatch();
    }

    async Task Verify(string name, BsonDocument description, BsonDocument[] required, CancellationToken cancellationToken)
    {
        VerifyOptions(description);
        using var cursor = await database.GetCollection<BsonDocument>(name).Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        if (indexes.Count != required.Length) throw new UniqueConstraintV2SchemaMismatch();
        foreach (var expected in required)
        {
            var actual = indexes.SingleOrDefault(index => index.GetValue("name", "") == expected["name"]);
            if (actual is null || !CompatibleIndex(actual, expected)) throw new UniqueConstraintV2SchemaMismatch();
        }
    }

    static void VerifyOptions(BsonDocument description)
    {
        var options = description.GetValue("options", new BsonDocument()).AsBsonDocument;
        if (description.GetValue("type", "") != "collection") throw new UniqueConstraintV2SchemaMismatch();
        if (!IsSimple(options)) throw new UniqueConstraintV2SchemaMismatch();
        if (options.Names.Any(field => field != "collation")) throw new UniqueConstraintV2SchemaMismatch();
    }

    static bool CompatibleIndex(BsonDocument actual, BsonDocument expected)
    {
        if (!actual["key"].Equals(expected["key"]) || !IsSimple(actual)) return false;
        if (actual.Names.Any(field => field is not ("v" or "key" or "name" or "ns" or "unique" or "collation"))) return false;
        return expected["name"] == "_id_" || actual.GetValue("unique", false) == expected["unique"];
    }
}
