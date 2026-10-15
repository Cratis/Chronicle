// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using MongoDB.Driver.Core;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Classifies failures exposed by the MongoDB driver.
/// </summary>
public class MongoDBStorageErrorClassifier : IStorageErrorClassifier
{
    /// <inheritdoc/>
    public bool IsTransient(Exception exception) =>
        exception is OperationCanceledException or TimeoutException or MongoExecutionTimeoutException or
            MongoConnectionException or MongoConnectionPoolPausedException or MongoProxyConnectionException or MongoWriteConcernException or
            MongoNotPrimaryException or MongoNodeIsRecoveringException or MongoCursorNotFoundException or
            MongoWriteException { WriteError: null, WriteConcernError: not null } or MongoBulkWriteException { WriteConcernError: not null } or
            ClientBulkWriteException { WriteConcernErrors.Count: > 0 } ||
        (exception is MongoException mongo && (mongo.HasErrorLabel("TransientTransactionError") || mongo.HasErrorLabel("UnknownTransactionCommitResult") || mongo.HasErrorLabel("RetryableWriteError") || mongo.HasErrorLabel("RetryableReadError"))) ||
        (exception is AggregateException aggregate
            ? aggregate.InnerExceptions.Any(IsTransient)
            : exception.InnerException is { } inner && IsTransient(inner));
}
