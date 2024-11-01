// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Defines a system for comparing objects.
/// </summary>
public interface IObjectComparer
{
    /// <summary>
    /// Compare two objects and get any differences in any properties.
    /// </summary>
    /// <param name="left">Left object to compare.</param>
    /// <param name="right">Right object to compare.</param>
    /// <param name="differences">Out variable with a collection of <see cref="PropertyDifference"/>.</param>
    /// <returns>True if they are the same, false if not.</returns>
    bool Compare(object? left, object? right, out IEnumerable<PropertyDifference> differences);
}
