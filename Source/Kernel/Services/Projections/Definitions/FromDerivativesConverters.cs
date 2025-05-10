// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromDerivatives"/>.
/// </summary>
internal static class FromDerivativesConverters
{
    /// <summary>
    /// Converts a <see cref="FromDerivatives"/> to its corresponding contract representation.
    /// </summary>
    /// <param name="fromEvery">The <see cref="FromDerivatives"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Projections.FromDerivativesDefinition"/>.</returns>
    public static Contracts.Projections.FromDerivativesDefinition ToContract(this FromDerivatives fromEvery)
    {
        return new()
        {
            EventTypes = fromEvery.EventTypes.Select(_ => _.ToContract()).ToList(),
            From = fromEvery.From.ToContract()
        };
    }

    /// <summary>
    /// Converts a contract representation of <see cref="FromDerivatives"/> to its corresponding Chronicle representation.
    /// </summary>
    /// <param name="fromAny">The contract representation of <see cref="FromDerivatives"/> to convert.</param>
    /// <returns>The converted <see cref="FromDerivatives"/>.</returns>
    public static FromDerivatives ToChronicle(this Contracts.Projections.FromDerivativesDefinition fromAny)
    {
        return new(
            fromAny.EventTypes.Select(_ => _.ToChronicle()),
            fromAny.From.ToChronicle()
        );
    }
}
