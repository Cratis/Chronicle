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
    const string SystemInformationCollectionName = "system-information";
    const int SystemInformationId = 0;

    /// <inheritdoc/>
    public IUserStorage Users { get; } = new UserStorage(database);

    /// <inheritdoc/>
    public IApplicationStorage Applications { get; } = new ApplicationStorage(database);

    /// <inheritdoc/>
    public IDataProtectionKeyStorage DataProtectionKeys { get; } = new DataProtectionKeyStorage(database);

    /// <inheritdoc/>
    public IPatchStorage Patches { get; } = new PatchStorage(database);

    /// <inheritdoc/>
    public async Task<SystemInformation?> GetSystemInformation()
    {
        var collection = database.GetCollection<MongoDBSystemInformation>(SystemInformationCollectionName);
        var cursor = await collection.FindAsync(si => si.Id == SystemInformationId);
        var document = await cursor.FirstOrDefaultAsync();
        return document is not null ? new SystemInformation(document.Version) : null;
    }

    /// <inheritdoc/>
    public async Task SetSystemInformation(SystemInformation systemInformation)
    {
        var collection = database.GetCollection<MongoDBSystemInformation>(SystemInformationCollectionName);
        var document = new MongoDBSystemInformation(SystemInformationId, systemInformation.Version);
        await collection.ReplaceOneAsync(
            si => si.Id == SystemInformationId,
            document,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<SemanticVersion?> GetVersion()
    {
        var systemInfo = await GetSystemInformation();
        return systemInfo?.Version;
    }

    /// <inheritdoc/>
    public async Task SetVersion(SemanticVersion version)
    {
        await SetSystemInformation(new SystemInformation(version));
    }
}
