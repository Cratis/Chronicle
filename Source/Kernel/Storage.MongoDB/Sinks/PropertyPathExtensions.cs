// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks;

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
}
