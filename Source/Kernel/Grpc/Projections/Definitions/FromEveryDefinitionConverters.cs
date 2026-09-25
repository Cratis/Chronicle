// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromEveryDefinition"/>.
/// </summary>
internal static class FromEveryDefinitionConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="FromEveryDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="FromEveryDefinition"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Contracts.Projections.FromEveryDefinition ToContract(this FromEveryDefinition definition)
    {
        return new()
        {
            Properties = definition.Properties.ToDictionary(_ => (string)_.Key, _ => _.Value),
            IncludeChildren = definition.IncludeChildren,
            Key = definition.Key
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="FromEveryDefinition"/>.
    /// </summary>
    /// <param name="contract"><see cref="Contracts.Projections.FromEveryDefinition"/> to convert.</param>
    /// <returns>Converted Chronicle version.</returns>
    public static FromEveryDefinition ToChronicle(this Contracts.Projections.FromEveryDefinition contract)
    {
        // A contract written before the key existed, or by a client that does not know about it, carries no key -
        // which has to mean the event source id rather than an empty expression the resolver cannot make sense of.
        return new(
            contract.Properties.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value),
            contract.IncludeChildren)
        {
            Key = string.IsNullOrEmpty(contract.Key) ? WellKnownExpressions.EventSourceId : contract.Key
        };
    }
}
