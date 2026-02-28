// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Extension methods for getting the system grain.
/// </summary>
public static class SystemExtensions
{
    /// <summary>
    /// Gets the system grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to use.</param>
    /// <returns>The <see cref="ISystem"/> grain.</returns>
    public static ISystem GetSystem(this IGrainFactory grainFactory) =>
        grainFactory.GetGrain<ISystem>(0);
}
