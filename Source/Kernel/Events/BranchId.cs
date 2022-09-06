// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the unique identifier of a branch from the event log.
/// </summary>
/// <param name="Value">Actual value.</param>
public record BranchId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// The <see cref="BranchId"/> representing an unspecified value.
    /// </summary>
    public static readonly BranchId Unspecified = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="BranchId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    /// <returns>A converted <see cref="BranchId"/>.</returns>;
    public static implicit operator BranchId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from a string representation of <see cref="Guid"/> to <see cref="BranchId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    /// <returns>A converted <see cref="BranchId"/>.</returns>;
    public static implicit operator BranchId(string id) => new(Guid.Parse(id));

    /// <summary>
    /// Create a new unique instance of <see cref="BranchId"/>.
    /// </summary>
    /// <returns>A new <see cref="BranchId"/> instance with a unique identifier.</returns>
    public static BranchId New() => new(Guid.NewGuid());
}
