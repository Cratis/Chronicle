// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelDefs = Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Converters for FromEventPropertyDefinition between MongoDB and Kernel representations.
/// </summary>
public static class FromEventPropertyDefinitionConverters
{
    /// <summary>
    /// Converts a MongoDB FromEventPropertyDefinition to a Kernel FromEventPropertyDefinition.
    /// </summary>
    /// <param name="source">The MongoDB FromEventPropertyDefinition to convert.</param>
    /// <returns>A Kernel FromEventPropertyDefinition.</returns>
    public static KernelDefs.FromEventPropertyDefinition ToKernel(this FromEventPropertyDefinition source) =>
        new(
            source.Event.ToKernel(),
            source.PropertyExpression);

    /// <summary>
    /// Converts a Kernel FromEventPropertyDefinition to a MongoDB FromEventPropertyDefinition.
    /// </summary>
    /// <param name="source">The Kernel FromEventPropertyDefinition to convert.</param>
    /// <returns>A MongoDB FromEventPropertyDefinition.</returns>
    public static FromEventPropertyDefinition ToMongoDB(this KernelDefs.FromEventPropertyDefinition source) =>
        new()
        {
            Event = source.Event.ToMongoDB(),
            PropertyExpression = source.PropertyExpression
        };
}
