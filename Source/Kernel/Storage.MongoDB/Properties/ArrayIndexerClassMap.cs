// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Properties;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Properties;

/// <summary>
/// Represents the BSON class map of <see cref="ArrayIndexer"/>, which is stored inside the replay state of an observer.
/// </summary>
/// <remarks>
/// <see cref="ArrayIndexer"/> lives outside the namespaces Chronicle camel cases, so without this map its element names are
/// decided by the naming policy Arc holds in process-wide state when the class map is first built. The names are pinned
/// here so that every host of the same database agrees on the stored shape.
/// </remarks>
public class ArrayIndexerClassMap : IBsonClassMapFor<ArrayIndexer>
{
    /// <summary>
    /// The element name the array property is stored under.
    /// </summary>
    public const string ArrayPropertyElementName = "arrayProperty";

    /// <summary>
    /// The element name the identifier property is stored under.
    /// </summary>
    public const string IdentifierPropertyElementName = "identifierProperty";

    /// <summary>
    /// The element name the identifier is stored under.
    /// </summary>
    public const string IdentifierElementName = "identifier";

    /// <inheritdoc/>
    public void Configure(BsonClassMap<ArrayIndexer> classMap)
    {
        classMap.AutoMap();
        classMap.GetMemberMap(_ => _.ArrayProperty).SetElementName(ArrayPropertyElementName);
        classMap.GetMemberMap(_ => _.IdentifierProperty).SetElementName(IdentifierPropertyElementName);
        classMap.GetMemberMap(_ => _.Identifier).SetElementName(IdentifierElementName);
    }
}
