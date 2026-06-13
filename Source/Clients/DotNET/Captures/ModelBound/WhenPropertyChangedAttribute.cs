// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event should be appended when a property changes.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WhenPropertyChangedAttribute"/>.
/// </remarks>
/// <param name="property">The property that must change.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WhenPropertyChangedAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the property that must change.
    /// </summary>
    public string Property { get; } = property;
}
