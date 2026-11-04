// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Binds shared runtime index models to a physical collection.
/// </summary>
/// <param name="Collection">The collection name.</param>
/// <param name="Names">Required index names.</param>
/// <param name="Obsolete">Obsolete index names.</param>
/// <param name="Existing">Lists existing index names.</param>
/// <param name="Ensure">Ensures the required indexes through the runtime helpers.</param>
/// <param name="Drop">Drops an obsolete index.</param>
internal record MongoDBIndexDefinition(
    string Collection,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Obsolete,
    Func<Task<IReadOnlySet<string>>> Existing,
    Func<Task> Ensure,
    Func<string, Task> Drop)
{
    /// <summary>
    /// Gets the accepted shapes for each required index name.
    /// </summary>
    internal required IReadOnlyDictionary<string, IReadOnlyList<BsonDocument>> Accepted { get; init; }

    /// <summary>
    /// Gets the inspection that lists full index definitions.
    /// </summary>
    internal required Func<Task<IReadOnlyList<BsonDocument>>> Inspect { get; init; }

    /// <summary>
    /// Binds typed index models to their collection without rendering them under different conventions.
    /// </summary>
    /// <typeparam name="T">The runtime document type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="models">The shared index models.</param>
    /// <param name="obsolete">Obsolete names, if any.</param>
    /// <param name="ensure">An optional specialized runtime ensure helper.</param>
    /// <param name="alternatives">Accepted alternative shapes for required names.</param>
    /// <returns>The bound definition.</returns>
    /// <exception cref="InvalidOperationException">An index has no name or an alternative does not name a required index.</exception>
    internal static MongoDBIndexDefinition For<T>(IMongoCollection<T> collection, IEnumerable<CreateIndexModel<T>> models, IReadOnlyList<string>? obsolete = null, Func<Task>? ensure = null, IEnumerable<CreateIndexModel<T>>? alternatives = null)
    {
        var indexes = models.ToArray();
        var names = indexes.Select(model => model.Options.Name ?? throw new InvalidOperationException("A provisioned MongoDB index requires an explicit name.")).ToArray();
        var accepted = indexes.ToDictionary(model => model.Options.Name, model => new List<BsonDocument> { MongoDBIndexShape.Render(model, collection) }, StringComparer.Ordinal);
        foreach (var alternative in alternatives ?? [])
        {
            if (alternative.Options.Name is not { } name || !accepted.TryGetValue(name, out var shapes))
            {
                throw new InvalidOperationException("An alternative MongoDB index must name a required index.");
            }
            shapes.Add(MongoDBIndexShape.Render(alternative, collection));
        }
        return new(
            collection.CollectionNamespace.CollectionName,
            names,
            obsolete ?? [],
            () => collection.GetIndexNamesAsync(),
            ensure ?? (() => collection.EnsureIndexesAsync(indexes)),
            name => collection.Indexes.DropOneAsync(name))
        {
            Accepted = accepted.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<BsonDocument>)pair.Value, StringComparer.Ordinal),
            Inspect = () => MongoDBIndexShape.List(collection, CancellationToken.None)
        };
    }
}
