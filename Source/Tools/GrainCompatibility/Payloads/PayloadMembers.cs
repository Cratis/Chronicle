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
    internal static IEnumerable<(string Name, Type Type, uint? Id, int Level, bool Constructor)> Read(Type type, PayloadKind kind) =>
        kind == PayloadKind.Orleans ? OrleansPayloadMembers.Read(type) : ReadJson(type);

    static IEnumerable<(string Name, Type Type, uint? Id, int Level, bool Constructor)> ReadJson(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var member in current.GetMembers(Flags))
            {
                if (!IsIncluded(member))
                {
                    continue;
                }

                var name = JsonName(member);
                if (seen.Add(name))
                {
                    var memberType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
                    yield return (name, memberType, null, 0, false);
                }
            }
        }
    }

    static bool IsIncluded(MemberInfo member)
    {
        if (member is not (PropertyInfo or FieldInfo))
        {
            return false;
        }

        if (member is PropertyInfo property && property.GetIndexParameters().Length > 0)
        {
            return false;
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
