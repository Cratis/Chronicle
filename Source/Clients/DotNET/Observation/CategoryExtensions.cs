// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for working with categories.
/// </summary>
public static class CategoryExtensions
{
    /// <summary>
    /// Get all categories from a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>Collection of category names.</returns>
    public static IEnumerable<string> GetCategories(this Type type)
    {
        var categoryAttributes = type.GetCustomAttributes<CategoryAttribute>();
        return categoryAttributes.SelectMany(_ => _.Categories);
    }
}
