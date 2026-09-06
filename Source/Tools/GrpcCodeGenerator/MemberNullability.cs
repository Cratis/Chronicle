// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Reads whether a member's value can be absent.
/// </summary>
/// <remarks>
/// The generated mappers take a value and read its properties, so mapping a member that is null is a null
/// reference. The declaration already says which members those are - it just does not say it in the
/// <see cref="Type"/>.
/// </remarks>
public static class MemberNullability
{
    static readonly NullabilityInfoContext _context = new();

    /// <summary>
    /// Determines whether a parameter's value can be absent.
    /// </summary>
    /// <param name="parameter">The parameter to read.</param>
    /// <returns>True when the value can be absent.</returns>
    public static bool Of(ParameterInfo parameter) => Of(parameter.ParameterType, () => _context.Create(parameter).ReadState);

    /// <summary>
    /// Determines whether a property's value can be absent.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <returns>True when the value can be absent.</returns>
    public static bool Of(PropertyInfo property) => Of(property.PropertyType, () => _context.Create(property).ReadState);

    static bool Of(Type type, Func<NullabilityState> state)
    {
        if (type.IsValueType)
        {
            return Nullable.GetUnderlyingType(type) is not null;
        }

        try
        {
            return state() == NullabilityState.Nullable;
        }
        catch (Exception ex)
        {
            // Nullability metadata references types the isolated context may not resolve. Treating the member as
            // always present matches how the mappers behaved before nullability was read at all.
            Console.WriteLine($"  WARNING: Could not read the nullability of a member: {ex.Message}");
            return false;
        }
    }
}
