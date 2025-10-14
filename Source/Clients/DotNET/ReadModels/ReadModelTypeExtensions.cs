// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extensions for working with read model types.
/// </summary>
public static class ReadModelTypeExtensions
{
    /// <summary>
    /// Gets the read model identifier for a given type.
    /// </summary>
    /// <param name="type">Type to get the read model identifier for.</param>
    /// <returns>Read model identifier.</returns>
    public static ReadModelIdentifier GetReadModelIdentifier(this Type type) => type.FullName ?? type.Name;
}
