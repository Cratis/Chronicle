// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents a value difference in a property of an object.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropertyDifference"/> class.
/// </remarks>
/// <param name="propertyPath">Raw difference.</param>
/// <param name="original">Original value.</param>
/// <param name="changed">Changed value.</param>
/// <param name="arrayIndexers">Array indexers.</param>
public class PropertyDifference(PropertyPath propertyPath, object? original, object? changed, ArrayIndexers? arrayIndexers = default)
{
    /// <summary>
    /// Gets the full member path to the property that has changed.
    /// </summary>
    public PropertyPath PropertyPath { get; } = propertyPath;

    /// <summary>
    /// Gets the original value - possibly default.
    /// </summary>
    public object? Original { get; } = original;

    /// <summary>
    /// Gets the changed value - possibly default.
    /// </summary>
    public object? Changed { get; } = changed;

    /// <summary>
    /// Gets the array indexers for the property.
    /// </summary>
    public ArrayIndexers ArrayIndexers { get; set; } = arrayIndexers ?? ArrayIndexers.NoIndexers;

    /// <summary>
    /// Gets a value indicating whether the difference is the removal of the property, rather than a new value for it.
    /// </summary>
    /// <remarks>
    /// A removal takes the property away entirely - for a dictionary entry the key is gone, it is not left holding null.
    /// <see cref="Changed"/> is always null for a removal.
    /// </remarks>
    public bool IsRemoval { get; private init; }

    /// <summary>
    /// Create a <see cref="PropertyDifference"/> that represents the removal of a property.
    /// </summary>
    /// <param name="propertyPath">The path of the property that was removed.</param>
    /// <param name="original">The value the property held before it was removed.</param>
    /// <param name="arrayIndexers">Array indexers.</param>
    /// <returns>A new <see cref="PropertyDifference"/> representing the removal.</returns>
    public static PropertyDifference Removal(PropertyPath propertyPath, object? original, ArrayIndexers? arrayIndexers = default) =>
        new(propertyPath, original, null, arrayIndexers) { IsRemoval = true };

    /// <summary>
    /// Create a copy of this difference with a different changed value, keeping everything else - including whether it is a removal.
    /// </summary>
    /// <param name="changed">The new changed value. Ignored for a removal.</param>
    /// <returns>A new <see cref="PropertyDifference"/>.</returns>
    public PropertyDifference WithChanged(object? changed) =>
        new(PropertyPath, Original, IsRemoval ? null : changed, ArrayIndexers) { IsRemoval = IsRemoval };

    /// <summary>
    /// Create a copy of this difference with different array indexers, keeping everything else - including whether it is a removal.
    /// </summary>
    /// <param name="arrayIndexers">The new <see cref="ArrayIndexers"/>.</param>
    /// <returns>A new <see cref="PropertyDifference"/>.</returns>
    public PropertyDifference WithArrayIndexers(ArrayIndexers arrayIndexers) =>
        new(PropertyPath, Original, Changed, arrayIndexers) { IsRemoval = IsRemoval };

    /// <summary>
    /// Apply the difference to a target object, either setting the changed value or removing the property.
    /// </summary>
    /// <param name="target">The target to apply to.</param>
    /// <param name="arrayIndexers">The <see cref="ArrayIndexers"/> to use, defaults to the ones held by the difference.</param>
    public void ApplyTo(object target, ArrayIndexers? arrayIndexers = default)
    {
        if (IsRemoval)
        {
            PropertyPath.RemoveValue(target, arrayIndexers ?? ArrayIndexers);
            return;
        }

        PropertyPath.SetValue(target, Changed!, arrayIndexers ?? ArrayIndexers);
    }

    /// <summary>
    /// Check if there are any changes.
    /// </summary>
    /// <returns>True if there is, false if not.</returns>
    public bool HasChanges() => IsRemoval || !Equals(Original, Changed);
}
