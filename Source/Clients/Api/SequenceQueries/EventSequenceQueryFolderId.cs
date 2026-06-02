// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the unique identifier of an event sequence query folder.
/// </summary>
/// <param name="Value">The underlying <see cref="Guid"/> value.</param>
public record EventSequenceQueryFolderId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the unset value.
    /// </summary>
    public static readonly EventSequenceQueryFolderId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="EventSequenceQueryFolderId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The <see cref="EventSequenceQueryFolderId"/> to convert from.</param>
    public static implicit operator Guid(EventSequenceQueryFolderId id) => id.Value;

    /// <summary>
    /// Implicitly convert from a <see cref="Guid"/> to a <see cref="EventSequenceQueryFolderId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator EventSequenceQueryFolderId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from a <see cref="EventSequenceQueryFolderId"/> to an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id">The <see cref="EventSequenceQueryFolderId"/> to convert from.</param>
    public static implicit operator EventSourceId(EventSequenceQueryFolderId id) => new(id.Value.ToString());

    /// <summary>
    /// Creates a new unique <see cref="EventSequenceQueryFolderId"/>.
    /// </summary>
    /// <returns>A new <see cref="EventSequenceQueryFolderId"/>.</returns>
    public static EventSequenceQueryFolderId New() => new(Guid.NewGuid());
}
