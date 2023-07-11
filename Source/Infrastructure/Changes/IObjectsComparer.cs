// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes;

/// <summary>
/// Defines a system for comparing objects.
/// </summary>
public interface IObjectsComparer
{
    /// <summary>
    /// Compare two objects and get any differences in any properties.
    /// </summary>
    /// <param name="left">Left object to compare.</param>
    /// <param name="right">Right object to compare.</param>
    /// <param name="differences">Out variable with a collection of <see cref="PropertyDifference"/>.</param>
    /// <returns>True if there are differences, false if not.</returns>
    bool Equals(object? left, object? right, out IEnumerable<PropertyDifference> differences);
}
