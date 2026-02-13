// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters for FromDerivatives between MongoDB and Kernel representations.
/// </summary>
public static class FromDerivativesConverters
{
    /// <summary>
    /// Converts a MongoDB FromDerivatives to a Kernel FromDerivatives.
    /// </summary>
    /// <param name="source">The MongoDB FromDerivatives to convert.</param>
    /// <returns>A Kernel FromDerivatives.</returns>
    public static KernelDefs.FromDerivatives ToKernel(this FromDerivatives source) =>
        new(
            source.EventTypes.Select(_ => _.ToKernel()).ToArray(),
            source.From.ToKernel());

    /// <summary>
    /// Converts a Kernel FromDerivatives to a MongoDB FromDerivatives.
    /// </summary>
    /// <param name="source">The Kernel FromDerivatives to convert.</param>
    /// <returns>A MongoDB FromDerivatives.</returns>
    public static FromDerivatives ToMongoDB(this KernelDefs.FromDerivatives source) =>
        new()
        {
            EventTypes = source.EventTypes.Select(_ => _.ToMongoDB()).ToArray(),
            From = source.From.ToMongoDB()
        };
}
