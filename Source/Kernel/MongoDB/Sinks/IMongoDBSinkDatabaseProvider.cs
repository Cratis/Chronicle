// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a system that can provide <see cref="IMongoDatabase"/>.
/// </summary>
public interface IMongoDBSinkDatabaseProvider
{
    /// <summary>
    /// Get the database for the ccurrent context.
    /// </summary>
    /// <returns><see cref="IMongoDatabase"/>.</returns>
    IMongoDatabase GetDatabase();
}
