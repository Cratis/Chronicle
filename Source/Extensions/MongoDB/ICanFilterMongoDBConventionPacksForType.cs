// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Conventions;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Defines a system that can tell whether or not a specific <see cref="Type"/> should be affected by a <see cref="IConventionPack"/>.
/// </summary>
public interface ICanFilterMongoDBConventionPacksForType
{
    /// <summary>
    /// Returns true if <see cref="Type"/> is affected, false if not.
    /// </summary>
    /// <param name="conventionPackName">Name of the <see cref="IConventionPack"/>.</param>
    /// <param name="conventionPack">The <see cref="IConventionPack"/>.</param>
    /// <param name="type"><see cref="Type"/> to consider.</param>
    /// <returns>True if affected, false if not.</returns>
    bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type);
}
