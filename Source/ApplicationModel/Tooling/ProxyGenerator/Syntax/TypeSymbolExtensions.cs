// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.ProxyGenerator.Templates;
using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax
{
    /// <summary>
    /// Extension methods for working with <see cref="ITypeSymbol"/>.
    /// </summary>
    public static class TypeSymbolExtensions
    {
        /// <summary>
        /// Gets the definition of any type.
        /// </summary>
        public const string AnyType = "any";

        static readonly Dictionary<string, TargetType> _primitiveTypeMap = new()
        {
            { typeof(string).FullName!, new("string") },
            { typeof(short).FullName!, new("number") },
            { typeof(int).FullName!, new("number") },
            { typeof(long).FullName!, new("number") },
            { typeof(ushort).FullName!, new("number") },
            { typeof(uint).FullName!, new("number") },
            { typeof(ulong).FullName!, new("number") },
            { typeof(float).FullName!, new("number") },
            { typeof(double).FullName!, new("number") },
            { typeof(DateTime).FullName!, new("Date") },
            { typeof(DateTimeOffset).FullName!, new("Date") },
            { typeof(Guid).FullName!, new("string") },
            { "System.Text.Json.Nodes", new("any") },
            { "System.Text.Json.JsonDocument", new("any") }
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
        /// Get <see cref="PropertyDescriptor">property descriptors</see> from all properties on a type.
        /// </summary>
        /// <param name="type"><see cref="ITypeSymbol"/> to get for.</param>
        /// <param name="additionalImportStatements">Any additional <see cref="ImportStatement">import statements</see> needed.</param>
        /// <returns>All <see cref="PropertyDescriptor">property descriptors</see> for type.</returns>
        public static IEnumerable<PropertyDescriptor> GetPropertyDescriptorsFrom(this ITypeSymbol type, out IEnumerable<ImportStatement> additionalImportStatements)
        {
            var descriptors = new List<PropertyDescriptor>();
            var allImportStatements = new HashSet<ImportStatement>();
            additionalImportStatements = allImportStatements;

            var properties = type.GetMembers().Where(_ => !_.IsStatic
                && _ is IPropertySymbol propertySymbol
                && propertySymbol.DeclaredAccessibility == Accessibility.Public).Cast<IPropertySymbol>();

            return properties.Select(_ =>
            {
                var returnType = _.GetMethod!.ReturnType;
                var descriptor = new PropertyDescriptor(
                    _.Name,
                    returnType.GetTypeScriptType(out var importStatements),
                    returnType.IsEnumerable());

                importStatements.ForEach(_ => allImportStatements.Add(_));
                return descriptor;
            }).ToArray();
        }

        /// <summary>
        /// Get the type script type string for a given <see cref="ITypeSymbol"/>.
        /// </summary>
        /// <param name="symbol"><see cref="ITypeSymbol"/> to get for.</param>
        /// <param name="additionalImportStatements">Any additional <see cref="ImportStatement">import statements</see> needed.</param>
        /// <returns>TypeScript type.</returns>
        public static string GetTypeScriptType(this ITypeSymbol symbol, out IEnumerable<ImportStatement> additionalImportStatements)
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
                return targetType.Type;
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
            return symbol.ToDisplayString().StartsWith("Aksio.Cratis.Applications.Queries.ClientObservable<", StringComparison.InvariantCulture);
        }

        static string GetTypeName(ITypeSymbol symbol)
        {
            if (symbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                symbol = arrayTypeSymbol.ElementType;
            }

            return $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}";
        }
    }
}
