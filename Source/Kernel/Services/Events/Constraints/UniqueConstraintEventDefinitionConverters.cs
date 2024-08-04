// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents converter methods for converting between <see cref="Contracts.Events.Constraints.UniqueConstraintEventDefinition"/> and <see cref="UniqueConstraintEventDefinition"/>.
/// </summary>
public static class UniqueConstraintEventDefinitionConverters
{
    /// <summary>
    /// Convert <see cref="Contracts.Events.Constraints.UniqueConstraintEventDefinition"/> to <see cref="UniqueConstraintEventDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="Contracts.Events.Constraints.UniqueConstraintEventDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="UniqueConstraintEventDefinition"/>.</returns>
    public static UniqueConstraintEventDefinition ToChronicle(this Contracts.Events.Constraints.UniqueConstraintEventDefinition definition) =>
        new(definition.EventType.ToChronicle(), definition.Property);
}
