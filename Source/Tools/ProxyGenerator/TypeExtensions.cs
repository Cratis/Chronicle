// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Concepts;
using Cratis.ProxyGenerator.Templates;
using Cratis.Queries;
using Cratis.Reflection;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for working with types.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets the definition of any type.
    /// </summary>
    public static readonly TargetType AnyType = new("any", "Object");

    /// <summary>
    /// Gets the definition of any type that is a final one.
    /// </summary>
    public static readonly TargetType AnyTypeFinal = new("any", "Object", Final: true);

    static readonly Dictionary<string, TargetType> _primitiveTypeMap = new()
    {
        { typeof(object).FullName!, AnyTypeFinal },
        { typeof(byte).FullName!, new("number", "Number") },
        { typeof(sbyte).FullName!, new("number", "Number") },
        { typeof(bool).FullName!, new("boolean", "Boolean") },
        { typeof(string).FullName!, new("string", "String") },
        { typeof(short).FullName!, new("number", "Number") },
        { typeof(int).FullName!, new("number", "Number") },
        { typeof(long).FullName!, new("number", "Number") },
        { typeof(ushort).FullName!, new("number", "Number") },
        { typeof(uint).FullName!, new("number", "Number") },
        { typeof(ulong).FullName!, new("number", "Number") },
        { typeof(float).FullName!, new("number", "Number") },
        { typeof(double).FullName!, new("number", "Number") },
        { typeof(decimal).FullName!, new("number", "Number") },
        { typeof(DateTime).FullName!, new("Date",  "Date") },
        { typeof(DateTimeOffset).FullName!, new("Date", "Date") },
        { typeof(Guid).FullName!, new("string", "String") },
        { "System.DateOnly", new("Date", "Date") },
        { "System.TimeOnly", new("Date", "Date") },
        { "System.Text.Json.Nodes", AnyTypeFinal },
        { "System.Text.Json.Nodes.JsonNode", AnyTypeFinal },
        { "System.Text.Json.Nodes.JsonObject", AnyTypeFinal },
        { "System.Text.Json.Nodes.JsonArray", AnyTypeFinal },
        { "System.Text.Json.JsonDocument", AnyTypeFinal },
        { "System.Uri", new("string", "String") }
    };

    /// <summary>
    /// Check whether or not a <see cref="Type"/> is a known type in TypeScript.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if it is known, false if not.</returns>
    public static bool IsKnownType(this Type type)
    {
        if (type.IsConcept())
        {
            type = type.GetConceptValueType();
        }

        return _primitiveTypeMap.ContainsKey(type.FullName!);
    }

    /// <summary>
    /// Get property descriptors for a type.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns>Collection of <see cref="PropertyDescriptor"/>.</returns>
    public static IEnumerable<PropertyDescriptor> GetPropertyDescriptors(this Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList().ConvertAll(_ => _.ToPropertyDescriptor());
    }

    /// <summary>
    /// Get target type for a type.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns>The <see cref="TargetType"/>.</returns>
    public static TargetType GetTargetType(this Type type)
    {
        if (type.IsConcept())
        {
            type = type.GetConceptValueType();
        }

        if (_primitiveTypeMap.TryGetValue(type.FullName!, out var value))
        {
            return value;
        }

        return new TargetType(type.Name, type.Name);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="TypeDescriptor"/>.
    /// </summary>
    /// <param name="type">Type to convert.</param>
    /// <returns>Converted <see cref="TypeDescriptor"/>.</returns>
    public static TypeDescriptor ToTypeDescriptor(this Type type)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
        var propertyDescriptors = properties.ConvertAll(_ => _.ToPropertyDescriptor());

        var typesInvolved = new List<Type>();
        typesInvolved.AddRange(propertyDescriptors.Select(_ => _.OriginalType).Where(t_ => !t_.IsKnownType()));

        return new TypeDescriptor(
            type,
            type.GetTargetType().Type,
            propertyDescriptors,
            [],
            typesInvolved);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="ModelDescriptor"/>.
    /// </summary>
    /// <param name="type">Type to convert.</param>
    /// <returns>Converted <see cref="ModelDescriptor"/>.</returns>
    public static ModelDescriptor ToModelDescriptor(this Type type)
    {
        var isEnumerable = type.IsEnumerable();
        if (isEnumerable)
        {
            type = type.GetEnumerableElementType()!;
        }

        var targetType = type.GetTargetType();

        return new(
            type,
            targetType.Type,
            targetType.Constructor,
            isEnumerable,
            type.IsObservable(),
            Enumerable.Empty<ImportStatement>());
    }

    /// <summary>
    /// Check if a type is observable.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is observable, false if not.</returns>
    public static bool IsObservable(this Type type) => type.IsAssignableTo(typeof(IClientObservable));

    /// <summary>
    /// Resolve the relative path for a type.
    /// </summary>
    /// <param name="type">Type to resolve for.</param>
    /// <returns>Resolved path.</returns>
    public static string ResolveTargetPath(this Type type) =>
        type.Namespace!.Replace(Globals.NamespacePrefix, string.Empty).Replace('.', Path.DirectorySeparatorChar);
}
