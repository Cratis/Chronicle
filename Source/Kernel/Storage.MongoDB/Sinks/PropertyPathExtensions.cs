// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Extensions for <see cref="PropertyPath"/>.
/// </summary>
public static class PropertyPathExtensions
{
    /// <summary>
    /// Checks whether or not a <see cref="PropertyPath"/> is the MongoDB key property (_id / id).
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to check.</param>
    /// <returns>True if it is the key property, false if not.</returns>
    public static bool IsMongoDBKey(this PropertyPath propertyPath) => propertyPath == "_id" || propertyPath == "id";

    /// <summary>
    /// Get the children property of a property path.
    /// </summary>
    /// <param name="property"><see cref="PropertyPath"/> to get from.</param>
    /// <returns>The new <see cref="PropertyPath"/>.</returns>
    public static PropertyPath GetChildrenProperty(this PropertyPath property)
    {
        var segments = property.Segments.ToArray();
        var childrenProperty = new PropertyPath(string.Empty);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            childrenProperty += segments[i].ToString()!;
        }

        childrenProperty += segments[^1].Value;
        return childrenProperty;
    }
}
