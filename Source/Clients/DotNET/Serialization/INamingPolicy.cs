// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Serialization;

/// <summary>
/// Defines a contract for naming policies used during serialization.
/// </summary>
public interface INamingPolicy
{
    /// <summary>
    /// Gets the JSON property naming policy.
    /// </summary>
    JsonNamingPolicy? JsonPropertyNamingPolicy { get; }

    /// <summary>
    /// Gets the read model name based on the naming policy.
    /// </summary>
    /// <param name="readModel">The read model type to convert.</param>
    /// <returns>The converted name.</returns>
    string GetReadModelName(Type readModel);

    /// <summary>
    /// Gets the property name based on the naming policy.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name.</returns>
    string GetPropertyName(string name);
}
