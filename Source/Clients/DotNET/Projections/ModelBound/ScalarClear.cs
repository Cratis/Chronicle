// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Decides whether a read model member can be cleared, and rejects the declaration when it cannot.
/// </summary>
/// <remarks>
/// The <c>CHR0048</c> analyzer reports the same rule at build time. This is the reflection-side gate for everything
/// the analyzer never sees - a read model from an assembly compiled without the Chronicle analyzers, or one whose
/// diagnostic was suppressed - so a declaration that cannot work fails loudly at projection construction rather
/// than quietly writing a type default.
/// </remarks>
static class ScalarClear
{
    /// <summary>
    /// Throws when a clear is declared for a member that cannot hold null.
    /// </summary>
    /// <param name="declaringType">The read model type declaring the member.</param>
    /// <param name="member">The member the clear was declared for.</param>
    /// <exception cref="CannotClearNonNullableMember">Thrown when the member cannot hold null.</exception>
    internal static void ThrowIfCannotHoldNull(Type declaringType, MemberInfo member)
    {
        if (member is not PropertyInfo property)
        {
            return;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        if (!CanHoldNull(property.PropertyType, nullability))
        {
            throw new CannotClearNonNullableMember(declaringType, property.Name, property.PropertyType);
        }
    }

    /// <summary>
    /// Throws when a clear is declared for a record parameter that cannot hold null.
    /// </summary>
    /// <param name="declaringType">The read model type declaring the parameter.</param>
    /// <param name="parameter">The parameter the clear was declared for.</param>
    /// <exception cref="CannotClearNonNullableMember">Thrown when the parameter cannot hold null.</exception>
    internal static void ThrowIfCannotHoldNull(Type declaringType, ParameterInfo parameter)
    {
        var nullability = new NullabilityInfoContext().Create(parameter);
        if (!CanHoldNull(parameter.ParameterType, nullability))
        {
            throw new CannotClearNonNullableMember(declaringType, parameter.Name!, parameter.ParameterType);
        }
    }

    /// <summary>
    /// Determines whether a member's declared type can hold null.
    /// </summary>
    /// <param name="memberType">The declared type of the member.</param>
    /// <param name="nullability">The <see cref="NullabilityInfo"/> for the member.</param>
    /// <returns>True when the member can hold null, false when it cannot.</returns>
    /// <remarks>
    /// A member compiled outside a nullable-aware context reports <see cref="NullabilityState.Unknown"/> for both
    /// states. That is opted out of the analysis rather than a promise of non-null, so it is treated as able to hold
    /// null - the declaration is the author's to make. A value type that is not <see cref="Nullable{T}"/> cannot hold
    /// null whatever the context says.
    /// </remarks>
    static bool CanHoldNull(Type memberType, NullabilityInfo nullability)
    {
        if (Nullable.GetUnderlyingType(memberType) is not null)
        {
            return true;
        }

        if (memberType.IsValueType)
        {
            return false;
        }

        return nullability.ReadState != NullabilityState.NotNull &&
               nullability.WriteState != NullabilityState.NotNull;
    }
}
