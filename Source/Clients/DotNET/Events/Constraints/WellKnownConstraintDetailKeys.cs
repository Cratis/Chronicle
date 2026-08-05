// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Well known constraint detail keys.
/// </summary>
public static class WellKnownConstraintDetailKeys
{
    /// <summary>
    /// The key for the constraint detail for the property name.
    /// </summary>
    public const string PropertyName = "PropertyName";

    /// <summary>
    /// The key for the constraint detail holding the value the offending property carried.
    /// </summary>
    /// <remarks>
    /// Constraints are validated after compliance has been applied, so a property marked as PII carries its
    /// encrypted form here rather than the value the caller appended. Every other property carries the value
    /// itself.
    /// </remarks>
    public const string PropertyValue = "PropertyValue";
}
