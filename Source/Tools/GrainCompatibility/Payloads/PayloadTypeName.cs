// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// The stable, whitespace-free display names used by payload format 1.
/// </summary>
public static class PayloadTypeName
{
    static readonly Dictionary<Type, string> _keywords = new()
    {
        [typeof(void)] = "void", [typeof(object)] = "object", [typeof(string)] = "string",
        [typeof(bool)] = "bool", [typeof(char)] = "char", [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte", [typeof(short)] = "short", [typeof(ushort)] = "ushort",
        [typeof(int)] = "int", [typeof(uint)] = "uint", [typeof(long)] = "long",
        [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
        [typeof(decimal)] = "decimal", [typeof(nint)] = "nint", [typeof(nuint)] = "nuint"
    };

    /// <summary>
    /// Displays a type without assembly versions or nullable reference annotations.
    /// </summary>
    /// <param name="type">The type to display.</param>
    /// <returns>The stable type name.</returns>
    public static string Of(Type type)
    {
        if (_keywords.TryGetValue(type, out var keyword))
        {
            return keyword;
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Of(underlying) + "?";
        }

        if (type.IsArray)
        {
            return Of(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }

        if (!type.IsGenericType)
        {
            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        var definition = type.GetGenericTypeDefinition();
        var name = string.Join('.', (definition.FullName ?? definition.Name).Split('+').Select(_ => _.Split('`')[0]));
        return name + "<" + string.Join(',', type.GetGenericArguments().Select(Of)) + ">";
    }
}
