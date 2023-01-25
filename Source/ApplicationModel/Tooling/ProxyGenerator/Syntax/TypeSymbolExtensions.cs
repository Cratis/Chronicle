// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.ProxyGenerator.Templates;
using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax;

/// <summary>
/// Extension methods for working with <see cref="ITypeSymbol"/>.
/// </summary>
public static class TypeSymbolExtensions
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
        { "System.Text.Json.JsonDocument", AnyTypeFinal }
    };

    /// <summary>
    /// Get all public instance <see cref="IMethodSymbol">methods</see> from a <see cref="ITypeSymbol"/>.
    /// </summary>
    /// <param name="type"><see cref="ITypeSymbol"/> to get for.</param>
    /// <returns>All methods.</returns>
    public static IEnumerable<IMethodSymbol> GetPublicInstanceMethodsFrom(this ITypeSymbol type) => type.GetMembers().Where(_ =>
                        !_.IsStatic &&
                        _ is IMethodSymbol methodSymbol &&
                        methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                        methodSymbol.MethodKind != MethodKind.Constructor).Cast<IMethodSymbol>();

    /// <summary>
    /// Get all public instance <see cref="IPropertySymbol">properties</see> from <see cref="ITypeSymbol"/>.
    /// </summary>
    /// <param name="type"><see cref="ITypeSymbol"/> to get for.</param>
    /// <returns>All properties.</returns>
    public static IEnumerable<PropertyOrParameterSymbol> GetPublicPropertiesFrom(this ITypeSymbol type)
    {
        var parameters = type
            .GetMembers()
            .Where(_ => !_.IsStatic && _ is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Constructor)
            .Cast<IMethodSymbol>()
            .SelectMany(_ => _.Parameters
                .Where(p => !p.Type.Equals(type, SymbolEqualityComparer.Default))
                .Select(p => new PropertyOrParameterSymbol(p)));

        var properties = type.GetMembers().Where(_ => !_.IsStatic
            && _ is IPropertySymbol propertySymbol
            && propertySymbol.DeclaredAccessibility == Accessibility.Public).Cast<IPropertySymbol>().Select(_ => new PropertyOrParameterSymbol(_));

        return parameters
            .Concat(properties)
            .OrderByDescending(_ => _.GetAttributes().Count())
            .GroupBy(_ => _.Name)
            .Select(_ => _.First());
    }

    /// <summary>
    /// Get the type script type string for a given <see cref="ITypeSymbol"/>.
    /// </summary>
    /// <param name="symbol"><see cref="ITypeSymbol"/> to get for.</param>
    /// <param name="additionalImportStatements">Any additional <see cref="ImportStatement">import statements</see> needed.</param>
    /// <returns>TypeScript type.</returns>
    public static TargetType GetTypeScriptType(this ITypeSymbol symbol, out IEnumerable<ImportStatement> additionalImportStatements)
    {
        var imports = new List<ImportStatement>();
        additionalImportStatements = imports;

        symbol = symbol.GetValueType();

        var typeName = GetTypeName(symbol);
        if (_primitiveTypeMap.ContainsKey(typeName))
        {
            var targetType = _primitiveTypeMap[typeName];

            if (!string.IsNullOrEmpty(targetType.ImportFromModule))
            {
                imports.Add(new(targetType.Type, targetType.ImportFromModule));
            }
            return targetType;
        }
        return AnyType;
    }

    /// <summary>
    /// Get the value type. If the <see cref="ITypeSymbol"/> is of a concept type, it will look for the underlying value type.
    /// </summary>
    /// <param name="symbol"><see cref="ITypeSymbol"/> to get for.</param>
    /// <returns>Resolved <see cref="ITypeSymbol"/>.</returns>
    public static ITypeSymbol GetValueType(this ITypeSymbol symbol)
    {
        var baseType = symbol.BaseType;
        while (baseType != default)
        {
            if (baseType.ContainingNamespace.ToDisplayString() == "System" &&
                baseType.Name == "Object")
            {
                break;
            }

            if (baseType.IsGenericType &&
                baseType.ContainingNamespace.ToDisplayString() == "Aksio.Cratis.Concepts" &&
                baseType.Name == "ConceptAs")
            {
                return baseType!.TypeArguments[0];
            }
            baseType = baseType.BaseType;
        }

        return symbol;
    }

    /// <summary>
    /// Check whether or not a <see cref="ITypeSymbol"/> is a known type in TypeScript.
    /// </summary>
    /// <param name="symbol"><see cref="ITypeSymbol"/> to check.</param>
    /// <returns>True if it is known, false if not.</returns>
    public static bool IsKnownType(this ITypeSymbol symbol)
    {
        symbol = symbol.GetValueType();
        var typeName = GetTypeName(symbol);
        return _primitiveTypeMap.ContainsKey(typeName);
    }

    /// <summary>
    /// Check whether or not a <see cref="ITypeSymbol"/> is an enumerable.
    /// </summary>
    /// <param name="symbol"><see cref="ITypeSymbol"/> to check.</param>
    /// <returns>True if it is enumerable, false if not.</returns>
    public static bool IsEnumerable(this ITypeSymbol symbol)
    {
        if (symbol is IArrayTypeSymbol) return true;
        if (symbol.IsKnownType()) return false;
        return symbol.AllInterfaces.Any(_ => _.ToDisplayString() == "System.Collections.IEnumerable");
    }

    /// <summary>
    /// Check whether or not a <see cref="ITypeSymbol"/> is an observable client.
    /// </summary>
    /// <param name="symbol"><see cref="ITypeSymbol"/> to check.</param>
    /// <returns>True if it is an observable client, false if not.</returns>
    public static bool IsObservableClient(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString().StartsWith("Aksio.Cratis.Applications.Queries.ClientObservable<");
    }

    /// <summary>
    /// Gets an <see cref="EnumDescriptor"/> from a type symbol which is an enum.
    /// </summary>
    /// <param name="type"><see cref="ITypeSymbol"/> to get for.</param>
    /// <returns><see cref="EnumDescriptor"/>.</returns>
    public static EnumDescriptor GetEnumDescriptor(this ITypeSymbol type)
    {
        var enumValues = type
                            .GetMembers()
                            .Where(_ => _ is IFieldSymbol).Cast<IFieldSymbol>()
                            .Select(_ => new EnumValueDescriptor(_.Name, int.Parse(_.ConstantValue?.ToString() ?? "0")));
        return new EnumDescriptor(type.Name, enumValues);
    }

    static string GetTypeName(ITypeSymbol symbol)
    {
        if (symbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            symbol = arrayTypeSymbol.ElementType;
        }

        if (symbol.Name.Equals("Nullable") && symbol is INamedTypeSymbol namedTypeSymbol)
        {
            symbol = namedTypeSymbol.TypeArguments[0];
        }

        return $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}";
    }
}
