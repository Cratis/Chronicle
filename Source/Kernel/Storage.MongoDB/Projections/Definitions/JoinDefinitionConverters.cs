// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters between MongoDB JoinDefinition and Kernel JoinDefinition.
/// </summary>
public static class JoinDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB JoinDefinition to a Kernel JoinDefinition.
    /// </summary>
    /// <param name="source">The MongoDB JoinDefinition to convert.</param>
    /// <returns>A Kernel JoinDefinition.</returns>
    public static KernelDefs.JoinDefinition ToKernel(this JoinDefinition source) =>
        new(
            source.On,
            source.Properties.ToDictionary(kv => (PropertyPath)kv.Key, kv => kv.Value),
            source.Key);

    /// <summary>
    /// Converts a Kernel JoinDefinition to a MongoDB JoinDefinition.
    /// </summary>
    /// <param name="source">The Kernel JoinDefinition to convert.</param>
    /// <returns>A MongoDB JoinDefinition.</returns>
    public static JoinDefinition ToMongoDB(this KernelDefs.JoinDefinition source) =>
        new()
        {
            On = source.On,
            Properties = source.Properties.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            Key = source.Key
        };
}
