// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Represents whether the server exposes development tools.
/// </summary>
/// <param name="IsAvailable">Whether the tools are available.</param>
[ReadModel]
public record DevelopmentToolsAvailability(bool IsAvailable)
{
    /// <summary>
    /// Gets whether the server exposes development tools.
    /// </summary>
    /// <returns>The <see cref="DevelopmentToolsAvailability"/>.</returns>
    internal static DevelopmentToolsAvailability AreDevelopmentToolsAvailable() => new(KernelStateResetter.IsAvailable);
}
