// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Reflection;

namespace Aksio.Cratis.Kernel.Grains;

/// <summary>
/// Extensions for <see cref="IGrain"/>.
/// </summary>
public static class GrainExtensions
{
    /// <summary>
    /// Gets the <see cref="Type"/> of the grain. The grain type is the interface that the grain implements.
    /// </summary>
    /// <param name="grain"><see cref="IGrain"/> to get from.</param>
    /// <returns>The interface type of the grain.</returns>
    /// <exception cref="InvalidGrainName">If there is not a matching interface.</exception>
    public static Type GetGrainType(this IGrain grain)
    {
        if (grain is IGrainType grainType) return grainType.GrainType;

        var type = grain.GetType();
        return type.AllBaseAndImplementingTypes().SingleOrDefault(_ => _.Name == $"I{type.Name}") ?? throw new InvalidGrainName(type);
    }
}
