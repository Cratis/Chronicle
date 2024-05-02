// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.MongoDB;

/// <summary>
/// <para>A convention that sets the element name the same as the member name with the first character lower cased unless it starts with an acronym.</para>
/// <para>Based on https://github.com/mongodb/mongo-csharp-driver/blob/f41c21efa6061f92c955748caaca123dae8e51ac/src/MongoDB.Bson/Serialization/Conventions/CamelCaseElementNameConvention.cs.</para>
/// </summary>
public class AcronymFriendlyCamelCaseElementNameConvention : ConventionBase, IMemberMapConvention
{
    /// <summary>
    /// Gets the name of the convention.
    /// </summary>
    public const string ConventionName = "Acronym-friendly camel case element name convention";

    /// <summary>
    /// Applies a modification to the member map, using the ToCamelCase method on the element name.
    /// </summary>
    /// <param name="memberMap">The member map.</param>
    public void Apply(BsonMemberMap memberMap)
    {
        var name = memberMap.MemberName;
        name = GetElementName(name);
        memberMap.SetElementName(name);
    }

    string GetElementName(string memberName) => memberName.ToCamelCase();
}
