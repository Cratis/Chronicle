// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters between MongoDB RemovedWithDefinition and Kernel RemovedWithDefinition.
/// </summary>
public static class RemovedWithDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB RemovedWithDefinition to a Kernel RemovedWithDefinition.
    /// </summary>
    /// <param name="source">The MongoDB RemovedWithDefinition to convert.</param>
    /// <returns>A Kernel RemovedWithDefinition.</returns>
    public static KernelDefs.RemovedWithDefinition ToKernel(this RemovedWithDefinition source) =>
        new(
            source.Key,
            source.ParentKey);

    /// <summary>
    /// Converts a Kernel RemovedWithDefinition to a MongoDB RemovedWithDefinition.
    /// </summary>
    /// <param name="source">The Kernel RemovedWithDefinition to convert.</param>
    /// <returns>A MongoDB RemovedWithDefinition.</returns>
    public static RemovedWithDefinition ToMongoDB(this KernelDefs.RemovedWithDefinition source) =>
        new()
        {
            Key = source.Key,
            ParentKey = source.ParentKey
        };
}
