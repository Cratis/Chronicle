// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.MongoDB;

/// <summary>
/// Represents the default artifacts for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultMongoDBArtifacts"/> class.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> for discovering types.</param>
public class DefaultMongoDBArtifacts(ITypes types) : IMongoDBArtifacts
{
    /// <inheritdoc/>
    public IEnumerable<Type> ClassMaps { get; } = types.FindMultiple(typeof(IBsonClassMapFor<>));

    /// <inheritdoc/>
    public IEnumerable<Type> ConventionPackFilters { get; } = types.FindMultiple(typeof(ICanFilterMongoDBConventionPacksForType));
}
