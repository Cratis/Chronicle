// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters for ChildrenDefinition between MongoDB and Kernel representations.
/// </summary>
public static class ChildrenDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB ChildrenDefinition to a Kernel ChildrenDefinition.
    /// </summary>
    /// <param name="source">The MongoDB ChildrenDefinition to convert.</param>
    /// <returns>A Kernel ChildrenDefinition equivalent to the MongoDB source.</returns>
    public static KernelDefs.ChildrenDefinition ToKernel(this ChildrenDefinition source) =>
        new(
            source.IdentifiedBy,
            source.From.ToDictionary(kv => EventType.Parse(kv.Key).ToKernel(), kv => kv.Value.ToKernel()),
            source.Join.ToDictionary(kv => EventType.Parse(kv.Key).ToKernel(), kv => kv.Value.ToKernel()),
            source.Children.ToDictionary(kv => (PropertyPath)kv.Key, kv => kv.Value.ToKernel()),
            source.FromEvery.ToKernel(),
            source.RemovedWith.ToDictionary(kv => EventType.Parse(kv.Key).ToKernel(), kv => kv.Value.ToKernel()),
            source.RemovedWithJoin.ToDictionary(kv => EventType.Parse(kv.Key).ToKernel(), kv => kv.Value.ToKernel()),
            source.FromEventProperty?.ToKernel());

    /// <summary>
    /// Converts a Kernel ChildrenDefinition to a MongoDB ChildrenDefinition.
    /// </summary>
    /// <param name="source">The Kernel ChildrenDefinition to convert.</param>
    /// <returns>A MongoDB ChildrenDefinition equivalent to the Kernel source.</returns>
    public static ChildrenDefinition ToMongoDB(this KernelDefs.ChildrenDefinition source) =>
        new()
        {
            IdentifiedBy = source.IdentifiedBy,
            From = source.From.ToDictionary(kv => kv.Key.ToMongoDB().ToString(), kv => kv.Value.ToMongoDB()),
            Join = source.Join.ToDictionary(kv => kv.Key.ToMongoDB().ToString(), kv => kv.Value.ToMongoDB()),
            Children = source.Children.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToMongoDB()),
            FromEvery = source.FromEvery.ToMongoDB(),
            RemovedWith = source.RemovedWith.ToDictionary(kv => kv.Key.ToMongoDB().ToString(), kv => kv.Value.ToMongoDB()),
            RemovedWithJoin = source.RemovedWithJoin.ToDictionary(kv => kv.Key.ToMongoDB().ToString(), kv => kv.Value.ToMongoDB()),
            FromEventProperty = source.FromEventProperty?.ToMongoDB()
        };
}
