// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters between MongoDB FromEveryDefinition and Kernel FromEveryDefinition.
/// </summary>
public static class FromEveryDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB FromEveryDefinition to a Kernel FromEveryDefinition.
    /// </summary>
    /// <param name="source">The MongoDB FromEveryDefinition to convert.</param>
    /// <returns>A Kernel FromEveryDefinition.</returns>
    public static KernelDefs.FromEveryDefinition ToKernel(this FromEveryDefinition source) =>
        new(
            source.Properties.ToDictionary(kv => (PropertyPath)kv.Key, kv => kv.Value),
            source.IncludeChildren);

    /// <summary>
    /// Converts a Kernel FromEveryDefinition to a MongoDB FromEveryDefinition.
    /// </summary>
    /// <param name="source">The Kernel FromEveryDefinition to convert.</param>
    /// <returns>A MongoDB FromEveryDefinition.</returns>
    public static FromEveryDefinition ToMongoDB(this KernelDefs.FromEveryDefinition source) =>
        new()
        {
            Properties = source.Properties.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            IncludeChildren = source.IncludeChildren
        };
}
