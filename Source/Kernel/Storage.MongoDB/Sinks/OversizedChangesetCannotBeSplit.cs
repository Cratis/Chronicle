// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a single changeset touches more documents than one fenced transaction may hold
/// and contains an operation that cannot safely be split.
/// </summary>
/// <param name="readModel">The <see cref="ReadModelIdentifier"/> of the read model.</param>
/// <param name="key">The <see cref="Key"/> of the read model instance.</param>
/// <param name="partition">The <see cref="Key"/> of the partition that fails.</param>
/// <param name="innerException">The <see cref="NonIdempotentWriteCannotBeSplit"/> raised by the fenced writes.</param>
public class OversizedChangesetCannotBeSplit(ReadModelIdentifier readModel, Key key, Key partition, NonIdempotentWriteCannotBeSplit innerException)
    : InvalidOperationException(
        $"The changeset for read model '{readModel}' with key '{key}' (partition '{partition}') touches more than {innerException.MaxBatchSize} documents " +
        $"(ReadModels.MaxBatchSize = {innerException.MaxBatchSize}) and contains operations that cannot be split safely: [{string.Join(", ", innerException.Operations)}] " +
        $"on collection '{innerException.Collection}'. Raise ReadModels.MaxBatchSize (Cratis__Chronicle__ReadModels__MaxBatchSize) above the number of documents " +
        "this changeset touches, within MongoDB's transaction limits, then retry the failed partition. See the Chronicle documentation page hosting/configuration/read-models.",
        innerException)
{
    /// <summary>
    /// Gets the <see cref="ReadModelIdentifier"/> of the read model.
    /// </summary>
    public ReadModelIdentifier ReadModel { get; } = readModel;

    /// <summary>
    /// Gets the <see cref="Key"/> of the read model instance.
    /// </summary>
    public Key Key { get; } = key;

    /// <summary>
    /// Gets the <see cref="Key"/> of the partition that fails.
    /// </summary>
    public Key Partition { get; } = partition;

    /// <summary>
    /// Gets the name of the destination collection.
    /// </summary>
    public string Collection => innerException.Collection;

    /// <summary>
    /// Gets the operations that cannot be split safely.
    /// </summary>
    public IReadOnlyList<string> Operations => innerException.Operations;

    /// <summary>
    /// Gets the maximum number of documents of one transaction.
    /// </summary>
    public int MaxBatchSize => innerException.MaxBatchSize;
}
