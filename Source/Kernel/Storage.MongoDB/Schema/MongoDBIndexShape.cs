// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Renders and compares the index keys and options owned by Chronicle.
/// </summary>
internal static class MongoDBIndexShape
{
    /// <summary>
    /// Renders an index using its runtime collection's serialization conventions.
    /// </summary>
    /// <typeparam name="T">The runtime document type.</typeparam>
    /// <param name="model">The shared index model.</param>
    /// <param name="collection">The runtime collection.</param>
    /// <returns>The keys and relevant options.</returns>
    internal static BsonDocument Render<T>(CreateIndexModel<T> model, IMongoCollection<T> collection)
    {
        var args = new RenderArgs<T>(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
        var shape = new BsonDocument("key", model.Keys.Render(args));
        if (model.Options.Unique is { } unique) shape.Add("unique", unique);
        if (model.Options.Sparse is { } sparse) shape.Add("sparse", sparse);
        if (model.Options.ExpireAfter is { } expireAfter) shape.Add("expireAfterSeconds", expireAfter.TotalSeconds);
        if (model.Options is CreateIndexOptions<T> { PartialFilterExpression: { } filter }) shape.Add("partialFilterExpression", filter.Render(args));
        if (model.Options.Collation is { } collation) shape.Add("collation", collation.ToBsonDocument());
        return shape;
    }

    /// <summary>
    /// Compares keys in order and effective options, ignoring server metadata.
    /// </summary>
    /// <param name="expected">The rendered expected shape.</param>
    /// <param name="listed">The server's listed index.</param>
    /// <returns>Whether the listed index has the expected keys and options.</returns>
    internal static bool Matches(BsonDocument expected, BsonDocument listed) =>
        KeysMatch(expected["key"].AsBsonDocument, listed["key"].AsBsonDocument) &&
        expected.GetValue("unique", false) == listed.GetValue("unique", false) &&
        expected.GetValue("sparse", false) == listed.GetValue("sparse", false) &&
        expected.GetValue("hidden", false) == listed.GetValue("hidden", false) &&
        listed.GetValue("hidden", false) == false &&
        ValuesMatch(expected.GetValue("expireAfterSeconds", BsonNull.Value), listed.GetValue("expireAfterSeconds", BsonNull.Value)) &&
        expected.GetValue("partialFilterExpression", BsonNull.Value).Equals(listed.GetValue("partialFilterExpression", BsonNull.Value)) &&
        CollationsMatch(expected, listed);

    /// <summary>
    /// Lists full index definitions, treating a missing collection as empty.
    /// </summary>
    /// <typeparam name="T">The runtime document type.</typeparam>
    /// <param name="collection">The collection to inspect.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The listed index definitions.</returns>
    internal static async Task<IReadOnlyList<BsonDocument>> List<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
    {
        try
        {
            using var cursor = await collection.Indexes.ListAsync(cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 26)
        {
            // A namespace without a collection has no indexes yet.
            return [];
        }
    }

    static bool KeysMatch(BsonDocument expected, BsonDocument listed) =>
        expected.ElementCount == listed.ElementCount &&
        expected.Elements.Zip(listed.Elements).All(pair =>
            string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) && ValuesMatch(pair.First.Value, pair.Second.Value));

    static bool ValuesMatch(BsonValue expected, BsonValue listed) =>
        expected.IsNumeric && listed.IsNumeric ? expected.ToDouble() == listed.ToDouble() : expected.Equals(listed);

    static bool CollationsMatch(BsonDocument expected, BsonDocument listed)
    {
        var expectedCollation = expected.GetValue("collation", new BsonDocument("locale", "simple")).AsBsonDocument;
        var listedCollation = listed.GetValue("collation", new BsonDocument("locale", "simple")).AsBsonDocument;
        return (expectedCollation.GetValue("locale", BsonNull.Value) == "simple" && listedCollation.GetValue("locale", BsonNull.Value) == "simple") ||
            expectedCollation.Equals(listedCollation);
    }
}
