// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Identities;

/// <summary>
/// Converters for identities.
/// </summary>
public static class IdentityConverters
{
    /// <summary>
    /// Convert from entity to identity.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <returns>Converted identity.</returns>
    public static Chronicle.Concepts.Identities.Identity ToIdentity(this Identity entity) =>
        new(entity.Subject, entity.Name, entity.UserName);

    /// <summary>
    /// Convert from identity to entity.
    /// </summary>
    /// <param name="identity">Identity to convert from.</param>
    /// <param name="id">Unique identifier for the entity.</param>
    /// <returns>Converted entity.</returns>
    public static Identity ToEntity(this Chronicle.Concepts.Identities.Identity identity, IdentityId id) =>
        new()
        {
            Id = id.Value,
            Subject = identity.Subject,
            Name = identity.Name,
            UserName = identity.UserName
        };
}
