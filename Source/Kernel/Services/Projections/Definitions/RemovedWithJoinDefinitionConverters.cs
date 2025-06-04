// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="RemovedWithJoinDefinition"/>.
/// </summary>
internal static class RemovedWithJoinDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="RemovedWithJoinDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="RemovedWithJoinDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.RemovedWithJoinDefinition ToContract(this RemovedWithJoinDefinition definition)
    {
        return new()
        {
            Key = definition.Key
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="RemovedWithDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.RemovedWithJoinDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static RemovedWithJoinDefinition ToChronicle(this Contracts.Projections.RemovedWithJoinDefinition contract)
    {
        return new(
            contract.Key
        );
    }
}
