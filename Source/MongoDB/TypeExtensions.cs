// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB;

/// <summary>
/// Extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Get the type string for a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <returns>A type string.</returns>
    public static string GetTypeString(this Type type) =>
        (type.Namespace?.StartsWith("System") ?? false) ?
            (type.FullName ?? type.Name) :
            $"{type.FullName ?? type.Name}, {type.Assembly.GetName().Name}";
}
