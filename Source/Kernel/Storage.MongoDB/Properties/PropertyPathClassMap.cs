// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Properties;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties;

/// <summary>
/// Represents the BSON class map of <see cref="PropertyPath"/>, which is stored inside the definitions that name properties.
/// </summary>
/// <remarks>
/// <see cref="PropertyPath"/> lives outside the namespaces Chronicle camel cases, so without this map its element name is
/// decided by the naming policy Arc happens to hold in process-wide state when the class map is first built: path
/// under the server's policy, Path under Arc's default. A kernel and the one it is upgraded from, or two hosts of
/// the same database, must agree on the stored shape, so the element name is pinned here.
/// </remarks>
public class PropertyPathClassMap : IBsonClassMapFor<PropertyPath>
{
    /// <summary>
    /// The element name the path is stored under.
    /// </summary>
    public const string PathElementName = "path";

    /// <inheritdoc/>
    public void Configure(BsonClassMap<PropertyPath> classMap)
    {
        classMap.AutoMap();
        classMap.GetMemberMap(_ => _.Path).SetElementName(PathElementName);
    }
}
