// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.DevelopmentTools;

/// <summary>
/// Represents whether the server exposes development tools.
/// </summary>
/// <param name="IsAvailable">Whether the server was built with development tools compiled in.</param>
/// <remarks>
/// This query exists in every build - it is how the workbench finds out whether to offer the
/// development tools page at all. The tools themselves are only compiled into development builds,
/// so on a production server this answers false and no reset endpoint exists to call.
/// </remarks>
[ReadModel]
public record DevelopmentToolsAvailability(bool IsAvailable)
{
    /// <summary>
    /// Gets whether the server exposes development tools.
    /// </summary>
    /// <returns>The <see cref="DevelopmentToolsAvailability"/>.</returns>
    public static DevelopmentToolsAvailability AreDevelopmentToolsAvailable() =>
#if DEVELOPMENT
        new(true);
#else
        new(false);
#endif
}
