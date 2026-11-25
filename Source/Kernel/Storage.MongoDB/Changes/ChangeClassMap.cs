// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Changes;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents the BSON class map of <see cref="Change"/>, which is stored inside read model changesets.
/// </summary>
/// <remarks>
/// <see cref="Change"/> lives outside the namespaces Chronicle camel cases, so without this map its element name is
/// decided by the naming policy Arc holds in process-wide state when the class map is first built. The name is pinned here
/// so that every host of the same database agrees on the stored shape.
/// </remarks>
public class ChangeClassMap : IBsonClassMapFor<Change>
{
    /// <summary>
    /// The element name the state is stored under.
    /// </summary>
    public const string StateElementName = "state";

    /// <inheritdoc/>
    public void Configure(BsonClassMap<Change> classMap)
    {
        classMap.AutoMap();
        classMap.GetMemberMap(_ => _.State).SetElementName(StateElementName);
    }
}
