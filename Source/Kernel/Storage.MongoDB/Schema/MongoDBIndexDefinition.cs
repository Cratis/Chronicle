// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Binds typed index models to their collection without rendering them under different conventions.
    /// </summary>
    /// <typeparam name="T">The runtime document type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="models">The shared index models.</param>
    /// <param name="obsolete">Obsolete names, if any.</param>
    /// <param name="ensure">An optional specialized runtime ensure helper.</param>
    /// <returns>The bound definition.</returns>
    internal static MongoDBIndexDefinition For<T>(IMongoCollection<T> collection, IEnumerable<CreateIndexModel<T>> models, IReadOnlyList<string>? obsolete = null, Func<Task>? ensure = null)
    {
        var indexes = models.ToArray();
        return new(
            collection.CollectionNamespace.CollectionName,
            indexes.Select(model => model.Options.Name ?? throw new InvalidOperationException("A provisioned MongoDB index requires an explicit name.")).ToArray(),
            obsolete ?? [],
            () => collection.GetIndexNamesAsync(),
            ensure ?? (() => collection.EnsureIndexesAsync(indexes)),
            name => collection.Indexes.DropOneAsync(name));
    }
}
