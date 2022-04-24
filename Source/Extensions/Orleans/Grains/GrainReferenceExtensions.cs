// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Orleans;

/// <summary>
/// Extension methods for Orleans Grain references.
/// </summary>
public static class GrainReferenceExtensions
{
#pragma warning disable MA0069
    /// <summary>
    /// Callback that can be used to override GetReference implementation. This is useful during testing.
    /// </summary>
    public static Func<IAddressable, object>? GetReferenceOverride;

    /// <summary>
    /// Converts this grain to a specific grain interface.
    /// </summary>
    /// <typeparam name="TGrainInterface">The type of the grain interface.</typeparam>
    /// <param name="grain">The grain to convert.</param>
    /// <returns>A strongly typed <c>GrainReference</c> of grain interface type TGrainInterface.</returns>
    public static TGrainInterface GetReference<TGrainInterface>(this IAddressable grain)
    {
        if (GetReferenceOverride is not null)
        {
            return (TGrainInterface)GetReferenceOverride(grain)!;
        }

        return grain.AsReference<TGrainInterface>();
    }
}
