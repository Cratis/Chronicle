// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.MongoDB.Patching;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISystemStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
public class SystemStorage(IDatabase database) : ISystemStorage
{
    const string VersionCollectionName = "system-version";
    const string VersionDocumentId = "current";

    /// <inheritdoc/>
    public IUserStorage Users { get; } = new UserStorage(database);

    /// <inheritdoc/>
    public IApplicationStorage Applications { get; } = new ApplicationStorage(database);

    /// <inheritdoc/>
    public IDataProtectionKeyStorage DataProtectionKeys { get; } = new DataProtectionKeyStorage(database);

    /// <inheritdoc/>
    public IPatchStorage Patches { get; } = new PatchStorage(database);

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetVersion()
    {
        var collection = database.GetCollection<VersionDocument>(VersionCollectionName);
        var cursor = await collection.FindAsync(v => v.Id == VersionDocumentId);
        var document = await cursor.FirstOrDefaultAsync();
        return document?.Version;
    }

    /// <inheritdoc/>
    public async Task SetVersion(SemanticVersion version)
    {
        var collection = database.GetCollection<VersionDocument>(VersionCollectionName);
        var document = new VersionDocument(VersionDocumentId, version);
        await collection.ReplaceOneAsync(
            v => v.Id == VersionDocumentId,
            document,
            new ReplaceOptions { IsUpsert = true });
    }

    sealed record VersionDocument(string Id, SemanticVersion Version);
}
