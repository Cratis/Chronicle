// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a sequence-slot collision or durable identities that prevented an atomic keyed batch append.
/// </summary>
public sealed class AppendManyStorageFailure : OneOfBase<DuplicateEventSequenceNumber, IdentitiesAlreadyDurable>
{
    AppendManyStorageFailure(OneOf<DuplicateEventSequenceNumber, IdentitiesAlreadyDurable> value) : base(value)
    {
    }

    /// <summary>
    /// Converts a sequence-slot collision to a batch failure.
    /// </summary>
    /// <param name="value">The slot collision.</param>
    /// <returns>The batch failure.</returns>
    public static implicit operator AppendManyStorageFailure(DuplicateEventSequenceNumber value) => new(value);

    /// <summary>
    /// Converts durable identities to a batch failure.
    /// </summary>
    /// <param name="value">The durable identities.</param>
    /// <returns>The batch failure.</returns>
    public static implicit operator AppendManyStorageFailure(IdentitiesAlreadyDurable value) => new(value);
}
