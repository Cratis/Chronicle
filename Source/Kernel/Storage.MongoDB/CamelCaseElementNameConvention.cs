// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a convention that applies camel case naming to BSON element names for members in a specific namespace.
/// </summary>
public class CamelCaseElementNameConvention : IMemberMapConvention
{
    /// <inheritdoc/>
    public string Name => "CamelCase";

    /// <inheritdoc/>
    public void Apply(BsonMemberMap memberMap) => memberMap.SetElementName(memberMap.MemberName.ToCamelCase());
}
