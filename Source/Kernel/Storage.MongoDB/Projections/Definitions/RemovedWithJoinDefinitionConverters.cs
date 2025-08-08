// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters between MongoDB RemovedWithJoinDefinition and Kernel RemovedWithJoinDefinition.
/// </summary>
public static class RemovedWithJoinDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB RemovedWithJoinDefinition to a Kernel RemovedWithJoinDefinition.
    /// </summary>
    /// <param name="source">The MongoDB RemovedWithJoinDefinition to convert.</param>
    /// <returns>A Kernel RemovedWithJoinDefinition.</returns>
    public static KernelDefs.RemovedWithJoinDefinition ToKernel(this RemovedWithJoinDefinition source) => new(source.Key);

    /// <summary>
    /// Converts a Kernel RemovedWithJoinDefinition to a MongoDB RemovedWithJoinDefinition.
    /// </summary>
    /// <param name="source">The Kernel RemovedWithJoinDefinition to convert.</param>
    /// <returns>A MongoDB RemovedWithJoinDefinition.</returns>
    public static RemovedWithJoinDefinition ToMongoDB(this KernelDefs.RemovedWithJoinDefinition source) =>
        new()
        {
            Key = source.Key
        };
}
