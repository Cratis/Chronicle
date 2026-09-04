// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Cuts;

/// <summary>
/// Represents the deterministic identifier of a read-model cut, computed from the exact request it was captured
/// for - the same request produces the same id, so a repeated request naturally resolves to the same manifest
/// instead of capturing the same content twice.
/// </summary>
/// <param name="Value">The actual value.</param>
public record ReadModelCutId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the representation of a not-set <see cref="ReadModelCutId"/>.
    /// </summary>
    public static readonly ReadModelCutId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReadModelCutId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    public static implicit operator ReadModelCutId(Guid value) => new(value);
}
