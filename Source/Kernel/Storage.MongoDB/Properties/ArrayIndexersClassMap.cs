// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Properties;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties;

/// <summary>
/// Represents the BSON class map of <see cref="ArrayIndexers"/>, which is stored inside the replay state of an observer.
/// </summary>
/// <remarks>
/// <see cref="ArrayIndexers"/> lives outside the namespaces Chronicle camel cases, so without this map its element name is
/// decided by the naming policy Arc holds in process-wide state when the class map is first built. The name is pinned here
/// so that every host of the same database agrees on the stored shape.
/// </remarks>
public class ArrayIndexersClassMap : IBsonClassMapFor<ArrayIndexers>
{
    /// <summary>
    /// The element name the indexers are stored under.
    /// </summary>
    public const string AllElementName = "all";

    /// <inheritdoc/>
    public void Configure(BsonClassMap<ArrayIndexers> classMap)
    {
        classMap.AutoMap();
        classMap.GetMemberMap(_ => _.All).SetElementName(AllElementName);
    }
}
