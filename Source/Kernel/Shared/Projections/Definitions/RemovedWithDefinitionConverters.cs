// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromEventPropertyDefinition"/>.
/// </summary>
public static class RemovedWithDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="RemovedWithDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="RemovedWithDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.RemovedWithDefinition ToContract(this RemovedWithDefinition definition)
    {
        return new()
        {
            Event = definition.Event.ToContract(),
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="RemovedWithDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.RemovedWithDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static RemovedWithDefinition ToChronicle(this Contracts.Projections.RemovedWithDefinition contract)
    {
        return new(
            contract.Event.ToChronicle()
        );
    }
}
