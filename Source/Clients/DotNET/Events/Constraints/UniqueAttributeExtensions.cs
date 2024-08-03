// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Extension methods for working with <see cref="UniqueAttribute"/>.
/// </summary>
public static class UniqueAttributeExtensions
{
    /// <summary>
    /// Get the constraint name for a type adorned with <see cref="UniqueAttribute"/>, defaults to type name if not explicitly defined.
    /// </summary>
    /// <param name="member">Type to get for that has <see cref="UniqueAttribute"/>.</param>
    /// <returns>Name of constraint.</returns>
    public static ConstraintName GetConstraintName(this MemberInfo member) => member.GetCustomAttribute<UniqueAttribute>()?.Name ?? member.Name;

    /// <summary>
    /// Get the constraint message for a type adorned with <see cref="UniqueAttribute"/>, defaults to type name if not explicitly defined.
    /// </summary>
    /// <param name="member">Type to get for that has <see cref="UniqueAttribute"/>.</param>
    /// <returns>Message for constraint, if defined, default if not.</returns>
    public static ConstraintViolationMessage GetConstraintMessage(this MemberInfo member) => member.GetCustomAttribute<UniqueAttribute>()?.Message ?? ConstraintViolationMessage.NotDefined;
}
