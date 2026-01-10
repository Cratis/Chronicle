// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extensions for <see cref="PropertyPath"/> and property-related string operations.
/// </summary>
public static class PropertyExtensions
{
    /// <summary>
    /// Convert a property name to MongoDB convention, converting "id" to "_id".
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>MongoDB-compatible property name.</returns>
    public static string ToMongoDBPropertyName(this string propertyName) =>
        propertyName.Equals("id", StringComparison.OrdinalIgnoreCase) ? "_id" : propertyName;

    /// <summary>
    /// Checks whether or not a <see cref="PropertyPath"/> is the MongoDB key property (_id / id).
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to check.</param>
    /// <returns>True if it is the key property, false if not.</returns>
    public static bool IsMongoDBKey(this PropertyPath propertyPath) => propertyPath == "_id" || propertyPath == "id";

    /// <summary>
    /// Convert a <see cref="PropertyPath"/> to a MongoDB-compatible path string.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to convert.</param>
    /// <returns>MongoDB-compatible path string.</returns>
    /// <remarks>
    /// This method converts a property path to a MongoDB-compatible format by:
    /// - Removing array brackets from segments (e.g., "[Configurations]" becomes "Configurations")
    /// - Converting "id" properties to "_id" to follow MongoDB conventions
    /// - Joining segments with dots
    /// Example: "[Configurations].Id" becomes "Configurations._id".
    /// </remarks>
    public static string ToMongoDB(this PropertyPath propertyPath)
    {
        var segments = propertyPath.Segments.Select(segment => segment.Value.ToMongoDBPropertyName());
        return string.Join('.', segments);
    }

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
