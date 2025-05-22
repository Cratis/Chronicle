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
    /// <param name="typesToCheck">Collection of types to get from.</param>
    /// <param name="internalType">The type that is supposed to be internal.</param>
    /// <returns>Collection of internal types.</returns>
    public static TypeDefinition[] GetTypesReferencingInternalType(
        this IEnumerable<TypeDefinition> typesToCheck,
        TypeDefinition internalType) =>
        typesToCheck.Where(_ =>
            _.FullName != internalType.FullName &&
            _.IsReferencedBy(internalType)).ToArray();

    /// <summary>
    /// Check if a type is referenced by another type publicly.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="referencedType">The type we want to check if is referenced.</param>
    /// <returns>True if it is referenced, false if not.</returns>
    public static bool IsReferencedBy(this TypeDefinition type, TypeDefinition referencedType)
    {
        if (!type.IsPublic)
        {
            return false;
        }
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
