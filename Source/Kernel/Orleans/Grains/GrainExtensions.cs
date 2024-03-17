// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Reflection;

namespace Orleans;

/// <summary>
/// Extension methods for Orleans Grain references.
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

    /// <summary>
    /// Converts this grain to a specific grain interface.
    /// </summary>
    /// <typeparam name="TGrainInterface">The type of the grain interface.</typeparam>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to create grains from.</param>
    /// <param name="grain">The grain to convert.</param>
    /// <returns>A strongly typed <c>GrainReference</c> of grain interface type TGrainInterface.</returns>
    public static TGrainInterface GetReference<TGrainInterface>(this IGrainFactory grainFactory, IGrainWithGuidCompoundKey grain)
    {
        var id = grain.GetPrimaryKey(out var keyExt);
        var grainResult = grainFactory.GetGrain(grain.GetGrainType(), id, keyExt);
        if (!(grainResult is IGrainBase))
        {
            // We're doing an assumption here that if the grain is not a grain base, it is probably coming from a test scenario
            // and we will assume is a grain reference, so we can just cast it.
            return (TGrainInterface)grainResult;
        }
        return grainResult.AsReference<TGrainInterface>();
    }
}
