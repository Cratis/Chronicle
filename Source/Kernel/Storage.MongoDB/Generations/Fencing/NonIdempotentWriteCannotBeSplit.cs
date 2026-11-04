// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// The exception that is thrown when a fenced write must be split to respect the batch cap but is not idempotent.
/// </summary>
/// <param name="collection">The name of the destination collection.</param>
/// <param name="operations">The operations that are not idempotent.</param>
/// <param name="maxBatchSize">The maximum number of documents of one transaction (ReadModels.MaxBatchSize).</param>
public class NonIdempotentWriteCannotBeSplit(string collection, IReadOnlyList<string> operations, int maxBatchSize)
    : InvalidOperationException(
        $"A fenced write to collection '{collection}' is part of a changeset touching more than {maxBatchSize} documents (ReadModels.MaxBatchSize = {maxBatchSize}), so it must be split. " +
        $"The operations [{string.Join(", ", operations)}] are not idempotent and could be applied twice if a split part were retried. The write was refused and not applied.")
{
    /// <summary>
    /// Gets the name of the destination collection.
    /// </summary>
    public string Collection { get; } = collection;

    /// <summary>
    /// Gets the operations that are not idempotent.
    /// </summary>
    public IReadOnlyList<string> Operations { get; } = operations;

    /// <summary>
    /// Gets the maximum number of documents of one transaction.
    /// </summary>
    public int MaxBatchSize { get; } = maxBatchSize;
}
