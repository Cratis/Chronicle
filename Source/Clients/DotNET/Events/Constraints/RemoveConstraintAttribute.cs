// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Attribute to adorn event types to indicate that appending this event releases a named constraint.
/// </summary>
/// <param name="constraintName">Name of the constraint to release.</param>
/// <remarks>
/// Place this attribute on the event type that signals removal of a domain object (e.g. a deletion event).
/// The named constraint will stop blocking future appends once this event is observed.
/// Multiple attributes may be applied to the same event type if it releases more than one constraint.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RemoveConstraintAttribute(string constraintName) : Attribute
{
    /// <summary>
    /// Gets the name of the constraint to release.
    /// </summary>
    public string ConstraintName { get; } = constraintName;
}
