// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.MongoDB;

namespace Aksio.Cratis.Kernel.MongoDB.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausedByStore"/> using MongoDB.
/// </summary>
[SingletonPerTenant]
public class MongoDBCausedByStore : ICausedByStore
{
    readonly ITenantDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBCausedByStore"/> class.
    /// </summary>
    /// <param name="database">The database for the current tenant.</param>
    public MongoDBCausedByStore(ITenantDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public Task<IImmutableList<CausedById>> GetChainFor(CausedBy causedBy) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<CausedBy> GetFor(IEnumerable<CausedById> chain) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<CausedBy> GetSingleFor(CausedById causedById) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<CausedById> GetSingleFor(CausedBy causedBy) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<bool> HasFor(CausedById causedById) => throw new NotImplementedException();
}

