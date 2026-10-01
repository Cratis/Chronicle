// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.for_ReadModels;

/// <summary>
/// Reads a read model's document straight out of the sink, bypassing the compliance release that every
/// normal read path performs.
/// </summary>
/// <remarks>
/// Compliance specs need this because a round trip alone cannot tell "encrypted then released" apart from
/// "never encrypted, nothing to release" — both return the original value. Only the document at rest
/// distinguishes them.
/// <para>
/// Inspecting the document is necessarily storage-specific, and the integration matrix runs every namespace
/// against MongoDB and three SQL backends. On a SQL backend there is no MongoDB container at all — touching
/// <c language="csharp">ChronicleFixture.ReadModels</c> there throws "Exposed port 27017/tcp is not mapped" and takes the whole
/// fixture down. So the read is skipped unless the run is backed by MongoDB, and the specs pair every
/// at-rest assertion with one that fails if this read was skipped on a run where it should have happened.
/// </para>
/// </remarks>
public static class StoredReadModelDocument
{
    /// <summary>
    /// Gets whether the current run stores read models where this helper can read them.
    /// </summary>
    /// <param name="fixture">The <see cref="ChronicleFixture"/> for the run.</param>
    /// <returns>True when the run is backed by MongoDB, false for the SQL backends.</returns>
    public static bool CanBeInspected(ChronicleFixture fixture) =>
        fixture.Options.StorageProvider == ChronicleStorageProvider.MongoDB;

    /// <summary>
    /// Reads the single document in a read model's collection, or null when the backend is not inspectable.
    /// </summary>
    /// <param name="fixture">The <see cref="ChronicleFixture"/> for the run.</param>
    /// <param name="collectionName">The name of the read model collection.</param>
    /// <returns>The stored <see cref="BsonDocument"/>, or null when the backend cannot be inspected.</returns>
    public static async Task<BsonDocument?> Read(ChronicleFixture fixture, string collectionName)
    {
        if (!CanBeInspected(fixture))
        {
            return null;
        }

        return await fixture.ReadModels.Database
            .GetCollection<BsonDocument>(collectionName)
            .Find(Builders<BsonDocument>.Filter.Empty)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Reads the single document in a read model's collection, waiting until it exists and satisfies a condition.
    /// </summary>
    /// <param name="fixture">The <see cref="ChronicleFixture"/> for the run.</param>
    /// <param name="collectionName">The name of the read model collection.</param>
    /// <param name="condition">The condition the stored document has to satisfy before it is returned.</param>
    /// <returns>The stored <see cref="BsonDocument"/>, or null when the backend cannot be inspected or the document never satisfied the condition.</returns>
    public static async Task<BsonDocument?> ReadWhen(ChronicleFixture fixture, string collectionName, Func<BsonDocument, bool> condition)
    {
        if (!CanBeInspected(fixture))
        {
            return null;
        }

        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        BsonDocument? document = null;
        while (!cts.IsCancellationRequested)
        {
            document = await Read(fixture, collectionName);
            if (document is not null && condition(document))
            {
                return document;
            }

            await Task.Delay(200);
        }

        return null;
    }

    /// <summary>
    /// Gets a field of a stored document by name, ignoring the casing the sink stores it with.
    /// </summary>
    /// <param name="document">The stored <see cref="BsonDocument"/>, or null.</param>
    /// <param name="name">The name of the field.</param>
    /// <returns>The <see cref="BsonValue"/>, or null when there is no document or no such field.</returns>
    public static BsonValue? Field(BsonDocument? document, string name) =>
        document?.Elements.FirstOrDefault(element => string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
}
