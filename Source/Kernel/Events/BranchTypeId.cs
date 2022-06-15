// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the unique identifier of a branch from the event log.
/// </summary>
/// <param name="Value">Actual value.</param>
public record BranchTypeId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// The <see cref="BranchTypeId"/> representing an unknown value.
    /// </summary>
    public static readonly BranchTypeId Unknown = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    /// <returns>A converted <see cref="BranchTypeId"/>.</returns>;
    public static implicit operator BranchTypeId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from a string representation of <see cref="Guid"/> to <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    /// <returns>A converted <see cref="BranchTypeId"/>.</returns>;
    public static implicit operator BranchTypeId(string id) => new(Guid.Parse(id));
}
