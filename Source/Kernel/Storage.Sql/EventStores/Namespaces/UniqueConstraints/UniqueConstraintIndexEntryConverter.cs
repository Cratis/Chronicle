// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Converter methods for working with <see cref="UniqueConstraintIndex"/> converting to and from SQL representations.
/// </summary>
public static class UniqueConstraintIndexEntryConverter
{
    /// <summary>
    /// Convert from <see cref="UniqueConstraintIndexEntry"/> to <see cref="UniqueConstraintIndex"/>.
    /// </summary>
    /// <param name="entry">Source <see cref="UniqueConstraintIndexEntry"/>.</param>
    /// <returns>Converted <see cref="UniqueConstraintIndex"/>.</returns>
    public static UniqueConstraintIndex ToUniqueConstraintIndex(this UniqueConstraintIndexEntry entry) =>
        new(
            entry.EventSourceId,
            entry.Value,
            (EventSequenceNumber)entry.SequenceNumber);

    /// <summary>
    /// Convert from <see cref="UniqueConstraintIndex"/> to <see cref="UniqueConstraintIndexEntry"/>.
    /// </summary>
    /// <param name="index">Source <see cref="UniqueConstraintIndex"/>.</param>
    /// <param name="constraintName">The <see cref="ConstraintName"/> for the entry (not currently used but reserved for future use).</param>
    /// <returns>Converted <see cref="UniqueConstraintIndexEntry"/>.</returns>
#pragma warning disable IDE0060 // Remove unused parameter - parameter reserved for future use
    public static UniqueConstraintIndexEntry ToUniqueConstraintIndexEntry(this UniqueConstraintIndex index, ConstraintName constraintName) =>
#pragma warning restore IDE0060
        new()
        {
            EventSourceId = index.EventSourceId.Value,
            Value = index.Value.Value,
            SequenceNumber = index.SequenceNumber.Value
        };
}
