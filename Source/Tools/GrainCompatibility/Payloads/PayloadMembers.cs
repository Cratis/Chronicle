// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Selects the members visible to the payload's codec.
/// </summary>
internal static class PayloadMembers
{
    internal static IEnumerable<(string Name, Type Type, int? Id)> Read(Type type, PayloadKind kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var member in current.GetMembers(Flags))
            {
                if (!IsIncluded(member, kind))
                {
                    continue;
                }

                var name = kind == PayloadKind.Orleans ? member.Name : JsonName(member);
                if (seen.Add(name))
                {
                    var memberType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
                    yield return (name, memberType, PayloadAttributes.Id(member));
                }
            }
        }
    }

    static bool IsIncluded(MemberInfo member, PayloadKind kind)
    {
        if (member is not (PropertyInfo or FieldInfo))
        {
            return false;
        }

        if (member is PropertyInfo property && property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        if (kind == PayloadKind.Orleans)
        {
            return PayloadAttributes.Id(member) is not null;
        }

        if (member.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.Always)
        {
            return false;
        }

        return Attribute.IsDefined(member, typeof(JsonIncludeAttribute))
            || member is PropertyInfo { GetMethod.IsPublic: true };
    }

    static string JsonName(MemberInfo member) =>
        member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(member.Name);
}
