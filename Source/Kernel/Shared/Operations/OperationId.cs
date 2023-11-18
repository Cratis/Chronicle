// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Represents the unique identifier of an operation.
/// </summary>
/// <param name="Value">Inner value.</param>
public record OperationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="OperationId"/>.
    /// </summary>
    public static readonly OperationId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="OperationId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> representation.</param>
    public static implicit operator OperationId(Guid id) => new(id);

    /// <summary>
    /// Create a new <see cref="OperationId"/>.
    /// </summary>
    /// <returns>A new <see cref="OperationId"/>.</returns>
    public static OperationId New() => new(Guid.NewGuid());
}
