// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Changes;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents the BSON class map of <see cref="ChildrenOrdering"/>, which is stored inside projection definitions.
/// </summary>
/// <remarks>
/// <see cref="ChildrenOrdering"/> lives outside the namespaces Chronicle camel cases, so without this map its element names are
/// decided by the naming policy Arc holds in process-wide state when the class map is first built. The names are pinned
/// here so that every host of the same database agrees on the stored shape.
/// </remarks>
public class ChildrenOrderingClassMap : IBsonClassMapFor<ChildrenOrdering>
{
    /// <summary>
    /// The element name the placement is stored under.
    /// </summary>
    public const string PlacementElementName = "placement";

    /// <summary>
    /// The element name the sort property is stored under.
    /// </summary>
    public const string SortByElementName = "sortBy";

    /// <summary>
    /// The element name the sort direction is stored under.
    /// </summary>
    public const string DirectionElementName = "direction";

    /// <inheritdoc/>
    public void Configure(BsonClassMap<ChildrenOrdering> classMap)
    {
        classMap.AutoMap();
        classMap.GetMemberMap(_ => _.Placement).SetElementName(PlacementElementName);
        classMap.GetMemberMap(_ => _.SortBy).SetElementName(SortByElementName);
        classMap.GetMemberMap(_ => _.Direction).SetElementName(DirectionElementName);
    }
}
