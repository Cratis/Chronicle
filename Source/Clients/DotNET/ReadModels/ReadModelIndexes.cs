// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Collects and validates read-model index declarations for registration.
/// </summary>
/// <param name="namingPolicy">The serialization naming policy.</param>
internal sealed class ReadModelIndexes(INamingPolicy namingPolicy)
{
    /// <summary>
    /// Collects indexes from a read model and its nested properties.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The wire index definitions.</returns>
    internal List<IndexDefinition> For(Type type)
    {
        var indexes = new List<IndexDefinition>();
        Collect(type, string.Empty, false, indexes, []);
        return indexes;
    }

    static string Prefix(string prefix, string path) => string.IsNullOrEmpty(prefix) ? path : $"{prefix}.{path}";

    void Collect(Type type, string prefix, bool throughArray, List<IndexDefinition> indexes, HashSet<Type> visited)
    {
        if (!visited.Add(type)) return;
        CollectComposites(type, prefix, throughArray, indexes);
        var constructor = type.GetConstructors().MaxBy(candidate => candidate.GetParameters().Length);
        var parameterIndexes = constructor?.GetParameters()
            .Where(parameter => parameter.IsDefined(typeof(IndexAttribute), inherit: false))
            .ToDictionary(parameter => parameter.Name!, parameter => parameter.GetCustomAttribute<IndexAttribute>()!) ?? [];
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var path = Prefix(prefix, namingPolicy.GetPropertyName(property.Name));
            var declaration = property.GetCustomAttribute<IndexAttribute>() ?? parameterIndexes.GetValueOrDefault(property.Name);
            if (declaration is not null)
            {
                indexes.Add(new()
                {
                    PropertyPath = path,
                    Properties = declaration.IsDescending ? [new() { Path = path, Descending = true }] : []
                });
            }
            var elementType = ReadModelIndexPath.ElementType(property.PropertyType);
            var nestedType = elementType ?? property.PropertyType;
            if (!nestedType.IsPrimitive && nestedType != typeof(string) && (elementType is not null || !nestedType.IsValueType))
            {
                Collect(nestedType, path, throughArray || elementType is not null, indexes, visited);
            }
        }
    }

    void CollectComposites(Type type, string prefix, bool throughArray, List<IndexDefinition> indexes)
    {
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var declaration in type.GetCustomAttributes<CompositeIndexAttribute>())
        {
            if (declaration.Properties is not { Length: > 0 })
            {
                throw new InvalidIndexDeclaration(type, $"{nameof(CompositeIndexAttribute)} must contain at least one property");
            }
            if (declaration.IsDescending is null || (declaration.IsDescending.Length > 0 && declaration.IsDescending.Length != declaration.Properties.Length))
            {
                throw new InvalidIndexDeclaration(type, $"{nameof(CompositeIndexAttribute.IsDescending)} length must match the number of properties");
            }
            var resolved = declaration.Properties.Select(path => ReadModelIndexPath.Resolve(type, path, namingPolicy)).ToArray();
            if (resolved.Select(path => path.Path).Distinct().Count() != resolved.Length)
            {
                throw new InvalidIndexDeclaration(type, "a composite index cannot repeat the same property");
            }
            if (resolved.Count(path => throughArray || path.ThroughArray) > 1)
            {
                throw new InvalidIndexDeclaration(type, "a composite index cannot contain more than one collection-element path");
            }
            var properties = resolved.Select((path, index) => new IndexProperty
            {
                Path = Prefix(prefix, path.Path),
                Descending = declaration.IsDescending.Length > 0 && declaration.IsDescending[index]
            }).ToArray();
            var signature = string.Join('|', properties.Select(property => $"{property.Path.Length}:{property.Path}:{property.Descending}"));
            if (!signatures.Add(signature))
            {
                throw new InvalidIndexDeclaration(type, $"duplicate composite index over '{string.Join(", ", properties.Select(property => property.Path))}'");
            }
            indexes.Add(new() { PropertyPath = properties[0].Path, Properties = properties });
        }
    }
}
