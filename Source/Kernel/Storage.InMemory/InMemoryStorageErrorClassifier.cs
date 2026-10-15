// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Classifies failures from the in-memory provider.
/// </summary>
public class InMemoryStorageErrorClassifier : IStorageErrorClassifier
{
    /// <inheritdoc/>
    public bool IsTransient(Exception exception) =>
        exception is OperationCanceledException or TimeoutException ||
        (exception is AggregateException aggregate
            ? aggregate.InnerExceptions.Any(IsTransient)
            : exception.InnerException is { } inner && IsTransient(inner));
}
