// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Carries the nullability an artifact declares onto the contract that describes it.
/// </summary>
/// <remarks>
/// A <see cref="Type"/> on its own cannot say whether a reference is nullable - the annotation lives in metadata on
/// the member, not in the type - so a contract generated from types alone declares every reference as always present.
/// Core declaring a member as optional while the contract declares it required is not a formatting difference: it makes
/// every mapping from that member a nullable-assignment warning, which is the generated code being told it is
/// wrong about something Core already knows.
/// </remarks>
public static class NullableAnnotations
{
    static readonly NullabilityInfoContext _context = new();

    /// <summary>
    /// Annotates a rendered type name with the nullability a parameter declares.
    /// </summary>
    /// <param name="typeName">The rendered type name.</param>
    /// <param name="parameter">The parameter the name was rendered from.</param>
    /// <returns>The annotated type name.</returns>
    public static string For(string typeName, ParameterInfo parameter) =>
        Annotate(typeName, parameter.ParameterType, () => _context.Create(parameter).ReadState);

    /// <summary>
    /// Determines whether a parameter's own declared reference type is nullable.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True when the parameter's reference type is annotated nullable.</returns>
    /// <remarks>
    /// A nullable value type is already discoverable from <see cref="Type"/> alone via <see cref="Nullable.GetUnderlyingType"/>
    /// - this exists for the reference-type case, where the annotation lives only in this metadata.
    /// </remarks>
    public static bool IsNullable(ParameterInfo parameter)
    {
        if (parameter.ParameterType.IsValueType)
        {
            return Nullable.GetUnderlyingType(parameter.ParameterType) is not null;
        }

        try
        {
            return _context.Create(parameter).ReadState == NullabilityState.Nullable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARNING: Could not read the nullability of '{parameter.Name}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Annotates a rendered type name with the nullability a property declares.
    /// </summary>
    /// <param name="typeName">The rendered type name.</param>
    /// <param name="property">The property the name was rendered from.</param>
    /// <returns>The annotated type name.</returns>
    public static string For(string typeName, PropertyInfo property) =>
        Annotate(typeName, property.PropertyType, () => _context.Create(property).ReadState);

    static string Annotate(string typeName, Type type, Func<NullabilityState> state)
    {
        // A nullable value type already renders as T? from the type alone.
        if (type.IsValueType || typeName.EndsWith('?'))
        {
            return typeName;
        }

        try
        {
            return state() == NullabilityState.Nullable ? $"{typeName}?" : typeName;
        }
        catch (Exception ex)
        {
            // Nullability metadata references types the isolated context may not resolve. Falling back to
            // non-nullable matches what the contracts declared before annotations were carried at all.
            Console.WriteLine($"  WARNING: Could not read the nullability of '{typeName}': {ex.Message}");
            return typeName;
        }
    }
}
