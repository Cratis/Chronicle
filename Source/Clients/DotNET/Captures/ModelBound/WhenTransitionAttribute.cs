// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event should be appended when a property transitions between two values.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WhenTransitionAttribute"/>.
/// </remarks>
/// <param name="property">The property that must transition.</param>
/// <param name="from">The value transitioned from.</param>
/// <param name="to">The value transitioned to.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WhenTransitionAttribute(string property, string from, string to) : Attribute
{
    /// <summary>
    /// Gets the property that must transition.
    /// </summary>
    public string Property { get; } = property;

    /// <summary>
    /// Gets the value transitioned from.
    /// </summary>
    public string From { get; } = from;

    /// <summary>
    /// Gets the value transitioned to.
    /// </summary>
    public string To { get; } = to;
}
