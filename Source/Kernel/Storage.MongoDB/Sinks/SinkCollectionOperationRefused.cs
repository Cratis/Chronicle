// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a destructive operation on a read model's collections is refused because it would
/// touch a collection the sink does not own in its current lifecycle phase.
/// </summary>
/// <param name="operation">The <see cref="SinkCollectionOperation"/> that was refused.</param>
/// <param name="collection">The collection the operation would have touched.</param>
/// <param name="reason">Why it was refused.</param>
public class SinkCollectionOperationRefused(SinkCollectionOperation operation, string collection, string reason)
    : Exception($"Refused to {operation.ToString().ToLowerInvariant()} collection '{collection}': {reason}")
{
    /// <summary>
    /// Gets the <see cref="SinkCollectionOperation"/> that was refused.
    /// </summary>
    public SinkCollectionOperation Operation { get; } = operation;

    /// <summary>
    /// Gets the collection the operation would have touched.
    /// </summary>
    public string Collection { get; } = collection;
}
