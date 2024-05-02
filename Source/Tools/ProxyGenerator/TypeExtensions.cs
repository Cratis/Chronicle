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
        { typeof(char).FullName!, new("string", "String") },
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
        { typeof(DateOnly).FullName!, new("Date", "Date") },
        { typeof(TimeOnly).FullName!, new("Date", "Date") },
        { typeof(System.Text.Json.Nodes.JsonNode).FullName!, AnyTypeFinal },
        { typeof(System.Text.Json.Nodes.JsonObject).FullName!, AnyTypeFinal },
        { typeof(System.Text.Json.Nodes.JsonArray).FullName!, AnyTypeFinal },
        { typeof(System.Text.Json.JsonDocument).FullName!, AnyTypeFinal },
        { typeof(Uri).FullName!, new("string", "String") }
    };

    /// <summary>
    /// Check whether or not a <see cref="Type"/> is a known type in TypeScript.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if it is known, false if not.</returns>
    public static bool IsKnownType(this Type type)
    {
        if (type.IsDictionary())
        {
            return true;
        }

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
        if (type.IsDictionary())
        {
            return AnyTypeFinal;
        }

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
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <returns>Converted <see cref="TypeDescriptor"/>.</returns>
    public static TypeDescriptor ToTypeDescriptor(this Type type, string targetPath)
    {
        var imports = new List<ImportStatement>();
        var typesInvolved = new List<Type>();

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
        var propertyDescriptors = properties.ConvertAll(_ => _.ToPropertyDescriptor());

        foreach (var property in propertyDescriptors.Where(_ => !_.OriginalType.IsKnownType()))
        {
            property.CollectTypesInvolved(typesInvolved);
        }

        return new TypeDescriptor(
            type,
            type.GetTargetType().Type,
            propertyDescriptors,
            typesInvolved.GetImports(targetPath, type!.ResolveTargetPath()),
            typesInvolved);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="ModelDescriptor"/>.
    /// </summary>
    /// <param name="type">Type to convert.</param>
    /// <returns>Converted <see cref="ModelDescriptor"/>.</returns>
    public static ModelDescriptor ToModelDescriptor(this Type type)
    {
        var isObservable = type.IsObservable();
        if (isObservable)
        {
            type = type.GetObservableElementType()!;
        }

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
            isObservable,
            []);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="EnumDescriptor"/>.
    /// </summary>
    /// <param name="type">Enum type to convert.</param>
    /// <returns>Converted <see cref="EnumDescriptor"/>.</returns>
    public static EnumDescriptor ToEnumDescriptor(this Type type)
    {
        var enumUnderlyingType = Enum.GetUnderlyingType(type);
        var values = Enum.GetValues(type).Cast<object>().Select(value => Convert.ChangeType(value, enumUnderlyingType)).ToArray();
        var names = Enum.GetNames(type);
        var members = values.Select((value, index) => new EnumMemberDescriptor(names[index], value)).ToArray();
        return new EnumDescriptor(type, type.Name, members, []);
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
    public static string ResolveTargetPath(this Type type)
    {
        var path = type.Namespace!.Replace(Globals.NamespacePrefix, string.Empty).Replace('.', Path.DirectorySeparatorChar);
        if (string.IsNullOrEmpty(path))
        {
            path = $"{Path.DirectorySeparatorChar}";
        }

        return path;
    }

    /// <summary>
    /// Get imports from a collection of types.
    /// </summary>
    /// <param name="types">Types to get from.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="relativePath">The relative path to work from.</param>
    /// <returns>A collection of <see cref="ImportStatement"/>.</returns>
    public static IEnumerable<ImportStatement> GetImports(this IEnumerable<Type> types, string targetPath, string relativePath) =>
         types.Select(_ =>
        {
            var fullPath = Path.Join(targetPath, relativePath);
            var fullPathForType = Path.Join(targetPath, _.ResolveTargetPath());
            var importPath = Path.GetRelativePath(fullPath, fullPathForType);
            importPath = $"{importPath}/{_.Name}";
            return new ImportStatement(_.GetTargetType().Type, importPath);
        }).ToArray();

    /// <summary>
    /// Collect types involved for a property, recursively.
    /// </summary>
    /// <param name="property">Property to collect for.</param>
    /// <param name="typesInvolved">Collected types involved.</param>
    /// <remarks>It skips any types already added to the collection passed to it.</remarks>
    public static void CollectTypesInvolved(this PropertyDescriptor property, IList<Type> typesInvolved)
    {
        if (typesInvolved.Contains(property.OriginalType)) return;
        typesInvolved.Add(property.OriginalType);
        foreach (var subProperty in property.OriginalType.GetPropertyDescriptors().Where(_ => !_.OriginalType.IsKnownType()))
        {
            CollectTypesInvolved(subProperty, typesInvolved);
        }
    }

    /// <summary>
    /// Collect types involved for an argument, recursively.
    /// </summary>
    /// <param name="argument">Argument to collect for.</param>
    /// <param name="typesInvolved">Collected types involved.</param>
    /// <remarks>It skips any types already added to the collection passed to it.</remarks>
    public static void CollectTypesInvolved(this RequestArgumentDescriptor argument, IList<Type> typesInvolved)
    {
        if (typesInvolved.Contains(argument.OriginalType)) return;
        typesInvolved.Add(argument.OriginalType);
        foreach (var subProperty in argument.OriginalType.GetPropertyDescriptors().Where(_ => !_.OriginalType.IsKnownType()))
        {
            CollectTypesInvolved(subProperty, typesInvolved);
        }
    }
}
