// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters for FromDefinition between MongoDB and Kernel representations.
/// </summary>
public static class FromDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB FromDefinition to a Kernel FromDefinition.
    /// </summary>
    /// <param name="source">The MongoDB FromDefinition to convert.</param>
    /// <returns>A Kernel FromDefinition.</returns>
    public static KernelDefs.FromDefinition ToKernel(this FromDefinition source) =>
        new(
            source.Properties.ToDictionary(kv => (PropertyPath)kv.Key, kv => kv.Value),
            source.Key,
            source.ParentKey);

    /// <summary>
    /// Converts a Kernel FromDefinition to a MongoDB FromDefinition.
    /// </summary>
    /// <param name="source">The Kernel FromDefinition to convert.</param>
    /// <returns>A MongoDB FromDefinition.</returns>
    public static FromDefinition ToMongoDB(this KernelDefs.FromDefinition source) =>
        new()
        {
            Properties = source.Properties.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            Key = source.Key,
            ParentKey = source.ParentKey
        };
}
