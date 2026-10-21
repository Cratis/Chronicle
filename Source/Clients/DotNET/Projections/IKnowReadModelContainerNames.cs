// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that knows which read models have a container name declared by their projection.
/// </summary>
internal interface IKnowReadModelContainerNames
{
    /// <summary>
    /// Get the container name a projection declared for a read model.
    /// </summary>
    /// <param name="readModelType">The read model type to get the container name for.</param>
    /// <returns>The declared container name, or null when the read model uses the one the naming policy gives it.</returns>
    string? GetContainerNameOverride(Type readModelType);
}
