// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Extension methods for working with <see cref="UniqueAttribute"/>.
/// </summary>
public static class UniqueAttributeExtensions
{
    /// <summary>
    /// Get the retention mode declared on a unique property constraint.
    /// </summary>
    /// <param name="member">Member to inspect.</param>
    /// <returns>The declared mode, defaulting to one value per event source.</returns>
    public static UniqueConstraintMode GetConstraintMode(this MemberInfo member) =>
        member.GetCustomAttribute<UniqueAttribute>()?.Mode ?? UniqueConstraintMode.PerEventSource;

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

    /// <summary>
    /// Get the event sequences declared by the <see cref="UniqueAttribute"/> on a member.
    /// </summary>
    /// <param name="member">Member to get for that has <see cref="UniqueAttribute"/>.</param>
    /// <returns>The <see cref="EventSequenceId"/> values declared, empty for every event sequence.</returns>
    public static IEnumerable<EventSequenceId> GetConstraintEventSequences(this MemberInfo member) =>
        member.GetCustomAttribute<UniqueAttribute>()?.EventSequences
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Distinct()
            .Select(_ => (EventSequenceId)_)
            .ToArray() ?? [];

    /// <summary>
    /// Get all <see cref="RemoveConstraintAttribute"/> instances applied to an event type.
    /// </summary>
    /// <param name="type">Event type to inspect.</param>
    /// <returns>All <see cref="RemoveConstraintAttribute"/> instances on the type.</returns>
    public static IEnumerable<RemoveConstraintAttribute> GetRemoveConstraints(this Type type) =>
        type.GetCustomAttributes<RemoveConstraintAttribute>();
}
