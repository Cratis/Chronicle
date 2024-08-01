// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromAnyDefinition"/>.
/// </summary>
public static class FromAnyDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="FromAnyDefinition"/> to its corresponding contract representation.
    /// </summary>
    /// <param name="fromAny">The <see cref="FromAnyDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Projections.FromAnyDefinition"/>.</returns>
    public static Contracts.Projections.FromAnyDefinition ToContract(this FromAnyDefinition fromAny)
    {
        return new()
        {
            EventTypes = fromAny.EventTypes.Select(_ => _.ToContract()).ToList(),
            From = fromAny.From.ToContract()
        };
    }

    /// <summary>
    /// Converts a contract representation of <see cref="FromAnyDefinition"/> to its corresponding Chronicle representation.
    /// </summary>
    /// <param name="fromAny">The contract representation of <see cref="FromAnyDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="FromAnyDefinition"/>.</returns>
    public static FromAnyDefinition ToChronicle(this Contracts.Projections.FromAnyDefinition fromAny)
    {
        return new(
            fromAny.EventTypes.Select(_ => _.ToChronicle()),
            fromAny.From.ToChronicle()
        );
    }
}
