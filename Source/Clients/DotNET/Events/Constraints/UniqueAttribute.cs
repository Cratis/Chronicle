// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Attribute to adorn types and properties on event types to indicate uniqueness.
/// </summary>
/// <param name="name">Optional name of the constraint to use.</param>
/// <param name="message">Optional message to use when the unique constraint is violated.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UniqueAttribute(string? name = default, string? message = default) : Attribute
{
    /// <summary>
    /// Gets the name of the constraint.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets the message to use when the unique constraint is violated.
    /// </summary>
    public string? Message { get; } = message;
}
