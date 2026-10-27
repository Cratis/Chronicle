// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_type_name
{
    internal static string Render(Type type, NullabilityInfo? nullable = null)
    {
        if (type.IsFunctionPointer)
        {
            throw new NotSupportedException("Function pointer signatures require an explicit snapshot renderer.");
        }

        if (type.HasElementType)
        {
            var suffix = ElementSuffix(type);
            return Render(type.GetElementType()!, type.IsArray ? nullable?.ElementType : nullable) + suffix + NullableSuffix(type, nullable);
        }

        var name = type.IsGenericParameter ? $"{(type.DeclaringMethod is null ? "!" : "!!")}{type.GenericParameterPosition}:{type.Name}" : Named(type, nullable);
        return name + NullableSuffix(type, nullable);
    }

    static string Named(Type type, NullabilityInfo? nullable)
    {
        var segments = new Stack<Type>();
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            segments.Push(current);
        }

        var arguments = type.GetGenericArguments();
        var position = 0;
        var names = new List<string>();
        foreach (var segment in segments)
        {
            var name = segment.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                var count = int.Parse(name[(tick + 1)..], System.Globalization.CultureInfo.InvariantCulture);
                var rendered = Enumerable.Range(position, count).Select(index => Render(arguments[index], nullable?.GenericTypeArguments.ElementAtOrDefault(index)));
                name = $"{name[..tick]}<{string.Join(',', rendered)}>";
                position += count;
            }

            names.Add(name);
        }

        return $"{type.Namespace}.{string.Join('+', names)}";
    }

    static string ElementSuffix(Type type) => type switch
    {
        { IsByRef: true } => "&",
        { IsPointer: true } => "*",
        { IsSZArray: true } => "[]",
        _ => type.GetArrayRank() == 1 ? "[*]" : $"[{new string(',', type.GetArrayRank() - 1)}]"
    };

    static string NullableSuffix(Type type, NullabilityInfo? nullable) =>
        type.IsValueType || type.IsPointer || type.IsByRef || nullable is null ? string.Empty : nullable.ReadState switch
        {
            NullabilityState.Nullable => "?",
            NullabilityState.NotNull => "!",
            _ => "~"
        };
}
