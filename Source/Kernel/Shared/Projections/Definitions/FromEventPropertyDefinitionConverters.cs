// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromEventPropertyDefinition"/>.
/// </summary>
public static class FromEventPropertyDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="FromEventPropertyDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="FromEventPropertyDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.FromEventPropertyDefinition ToContract(this FromEventPropertyDefinition definition)
    {
        return new()
        {
            Event = definition.Event.ToContract(),
            PropertyExpression = definition.PropertyExpression
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="FromEventPropertyDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.FromEventPropertyDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static FromEventPropertyDefinition ToChronicle(this Contracts.Projections.FromEventPropertyDefinition contract)
    {
        return new(
            contract.Event.ToChronicle(),
            contract.PropertyExpression
        );
    }
}
