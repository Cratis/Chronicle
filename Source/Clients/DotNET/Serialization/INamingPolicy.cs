// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serialization;

/// <summary>
/// Defines a contract for naming policies used during serialization.
/// </summary>
public interface INamingPolicy
{
    /// <summary>
    /// Converts the specified name to the desired format.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name.</returns>
    string ConvertName(string name);
}
