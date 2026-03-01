// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converter methods for <see cref="FromDefinition"/>.
/// </summary>
internal static class FromDefinitionConverters
{
    /// <summary>
    /// Converts a <see cref="FromDefinition"/> to its corresponding contract representation.
    /// </summary>
    /// <param name="definition">The <see cref="FromDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Projections.FromDefinition"/>.</returns>
    public static Contracts.Projections.FromDefinition ToContract(this FromDefinition definition)
    {
        return new()
        {
            Properties = definition.Properties.ToDictionary(_ => (string)_.Key, _ => _.Value),
            Key = definition.Key,
            ParentKey = definition.ParentKey ?? PropertyExpression.NotSet
        };
    }

    /// <summary>
    /// Converts a contract representation of <see cref="Contracts.Projections.FromDefinition"/> to its corresponding <see cref="FromDefinition"/>.
    /// </summary>
    /// <param name="contract">The contract representation of <see cref="Contracts.Projections.FromDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="FromDefinition"/>.</returns>
    public static FromDefinition ToChronicle(this Contracts.Projections.FromDefinition contract)
    {
        return new(
            contract.Properties.ToDictionary(_ => new PropertyPath(_.Key), _ => _.Value),
            contract.Key,
            contract.ParentKey ?? PropertyExpression.NotSet
        );
    }
}
