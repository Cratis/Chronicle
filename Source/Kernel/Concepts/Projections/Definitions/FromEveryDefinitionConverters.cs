// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromEveryDefinition"/>.
/// </summary>
public static class FromEveryDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="FromEveryDefinition"/> to its corresponding contract representation.
    /// </summary>
    /// <param name="fromEvery">The <see cref="FromEveryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Projections.FromEveryDefinition"/>.</returns>
    public static Contracts.Projections.FromEveryDefinition ToContract(this FromEveryDefinition fromEvery)
    {
        return new()
        {
            EventTypes = fromEvery.EventTypes.Select(_ => _.ToContract()).ToList(),
            From = fromEvery.From.ToContract()
        };
    }

    /// <summary>
    /// Converts a contract representation of <see cref="FromEveryDefinition"/> to its corresponding Chronicle representation.
    /// </summary>
    /// <param name="fromAny">The contract representation of <see cref="FromEveryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="FromEveryDefinition"/>.</returns>
    public static FromEveryDefinition ToChronicle(this Contracts.Projections.FromEveryDefinition fromAny)
    {
        return new(
            fromAny.EventTypes.Select(_ => _.ToChronicle()),
            fromAny.From.ToChronicle()
        );
    }
}
