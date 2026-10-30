// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Applies unique constraint index changes one at a time, through the single-item operations of
/// <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
/// <remarks>
/// This is the reference behavior for <see cref="IUniqueConstraintsBatchStorage.ApplyChanges"/>.
/// </remarks>
public static class UniqueConstraintIndexChanges
{
    /// <summary>
    /// Applies the changes in order, one at a time.
    /// </summary>
    /// <param name="storage">The <see cref="IUniqueConstraintsStorage"/> to apply them to.</param>
    /// <param name="definition">The <see cref="UniqueConstraintDefinition"/> the changes belong to.</param>
    /// <param name="changes">The changes, in the order the events were appended.</param>
    /// <param name="cancellationToken">Cancellation of the writes.</param>
    /// <returns>The positions, in <paramref name="changes"/>, of the claims refused because another event source holds the value.</returns>
    public static async Task<IReadOnlyCollection<int>> ApplySequentially(
        IUniqueConstraintsStorage storage,
        UniqueConstraintDefinition definition,
        IReadOnlyList<UniqueConstraintIndexChange> changes,
        CancellationToken cancellationToken = default)
    {
        var duplicates = new List<int>();
        for (var index = 0; index < changes.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var change = changes[index];
            try
            {
                await Apply(storage, definition, change, cancellationToken);
            }
            catch (DuplicateUniqueConstraintValue)
            {
                duplicates.Add(index);
            }
        }

        return duplicates;
    }

    /// <summary>
    /// Applies a single change through the single-item operations.
    /// </summary>
    /// <param name="storage">The <see cref="IUniqueConstraintsStorage"/> to apply it to.</param>
    /// <param name="definition">The <see cref="UniqueConstraintDefinition"/> the change belongs to.</param>
    /// <param name="change">The <see cref="UniqueConstraintIndexChange"/> to apply.</param>
    /// <param name="cancellationToken">Cancellation of the write.</param>
    /// <returns>The write task.</returns>
    /// <exception cref="DuplicateUniqueConstraintValue">A claimed value is held by another event source.</exception>
    public static Task Apply(IUniqueConstraintsStorage storage, UniqueConstraintDefinition definition, UniqueConstraintIndexChange change, CancellationToken cancellationToken = default) =>
        change.Kind switch
        {
            UniqueConstraintIndexChangeKind.Save => storage.Save(change.EventSourceId, definition, change.SequenceNumber, change.Value!, change.ScopeKey, change.Subject, cancellationToken),
            UniqueConstraintIndexChangeKind.RemoveValue => storage.RemoveValue(change.EventSourceId, definition, change.Value!, change.ScopeKey, cancellationToken),
            _ => storage.Remove(change.EventSourceId, definition, change.ScopeKey, cancellationToken)
        };
}
