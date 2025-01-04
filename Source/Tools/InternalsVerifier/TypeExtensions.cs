// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace InternalsVerifier;

/// <summary>
/// Represents a set of extensions for working with types in the context of Mono.Cecil.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Get the types referencing an internal type, directly or indirectly.
    /// </summary>
    /// <param name="assembly">Assembly to get from.</param>
    /// <param name="internalType">The type that is supposed to be internal.</param>
    /// <param name="internalNamespace">The internal namespace to ignore from.</param>
    /// <returns>Collection of internal types.</returns>
    public static TypeDefinition[] GetTypesReferencingInternalType(this AssemblyDefinition assembly, TypeDefinition internalType, string internalNamespace)
    {
        var referencingTypes = assembly.MainModule.Types.Where(_ => _.IsReferencedBy(internalType) && _.FullName != internalType.FullName).ToArray();
        var nonInternalTypes = referencingTypes.Where(_ => !_.Namespace.StartsWith(internalNamespace));
        if (nonInternalTypes.Any())
        {
            return nonInternalTypes.ToArray();
        }

        IEnumerable<TypeDefinition> result = [];
        foreach (var type in referencingTypes.Where(_ => _.Namespace.StartsWith(internalNamespace)))
        {
            var types = GetTypesReferencingInternalType(assembly, type, internalNamespace);
            if (types.Length > 0)
            {
                result = result.Concat(types);
            }
        }

        return result.Distinct().ToArray();
    }

    /// <summary>
    /// Check if a type is referenced by another type publicly.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="referencedType">The type we want to check if is referenced.</param>
    /// <returns>True if it is referenced, false if not.</returns>
    public static bool IsReferencedBy(this TypeDefinition type, TypeDefinition referencedType)
    {
        return
            type.Fields.Any(_ => _.IsPublic && _.FieldType.IsReferencedBy(referencedType)) ||
            type.Properties.Any(_ => _.GetMethod.IsPublic && _.PropertyType.IsReferencedBy(referencedType)) ||
            type.Methods.Any(_ => _.IsPublic && (_.ReturnType.IsReferencedBy(referencedType) || _.Parameters.Any(p => p.ParameterType.IsReferencedBy(referencedType)))) ||
            type.GetConstructors().Any(_ => _.IsPublic && _.Parameters.Any(p => p.ParameterType.IsReferencedBy(referencedType)));
    }

    /// <summary>
    /// Check if a type is referencing another  type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="referencedType">The type we want to check if is referenced.</param>
    /// <returns>True if it is referenced, false if not.</returns>
    public static bool IsReferencedBy(this TypeReference type, TypeDefinition referencedType)
    {
        return type.FullName == referencedType.FullName ||
            (type is GenericInstanceType genericType && genericType.GenericArguments.Any(_ => _.IsReferencedBy(referencedType)));
    }
}
