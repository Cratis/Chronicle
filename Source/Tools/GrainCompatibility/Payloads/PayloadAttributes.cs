// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Reads Orleans attributes without taking a dependency on Orleans.
/// </summary>
internal static class PayloadAttributes
{
    internal static CustomAttributeData? Find(MemberInfo member, string fullName) =>
        CustomAttributeData.GetCustomAttributes(member).FirstOrDefault(_ => _.AttributeType.FullName == fullName);

    internal static int? Id(MemberInfo member)
    {
        var attribute = Find(member, "Orleans.IdAttribute");
        return attribute is null ? null : Convert.ToInt32(attribute.ConstructorArguments[0].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string? Alias(Type type) =>
        Find(type, "Orleans.AliasAttribute")?.ConstructorArguments[0].Value as string;
}
