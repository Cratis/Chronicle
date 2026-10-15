// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBStorageErrorClassifier;

public class when_classifying_failures : Specification
{
    readonly MongoDBStorageErrorClassifier _classifier = new();
    readonly ConnectionId _connection = new(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));

    [Fact] void should_retry_cancellation() => _classifier.IsTransient(new OperationCanceledException()).ShouldBeTrue();
    [Fact] void should_retry_timeouts() => _classifier.IsTransient(new TimeoutException()).ShouldBeTrue();
    [Fact] void should_retry_nested_transient_errors() => _classifier.IsTransient(new InvalidOperationException("outer", new TimeoutException())).ShouldBeTrue();
    [Fact] void should_inspect_all_aggregate_errors() => _classifier.IsTransient(new AggregateException(new FormatException(), new TimeoutException())).ShouldBeTrue();
    [Fact] void should_retry_connection_errors() => _classifier.IsTransient(new MongoConnectionException(_connection, "connection")).ShouldBeTrue();
    [Fact] void should_retry_closed_connections() => _classifier.IsTransient(new MongoConnectionClosedException(_connection)).ShouldBeTrue();
    [Fact] void should_retry_paused_connection_pools() => _classifier.IsTransient(new MongoConnectionPoolPausedException("paused")).ShouldBeTrue();
    [Fact] void should_retry_proxy_connection_errors() => _classifier.IsTransient(new MongoProxyConnectionException("proxy connection", new IOException())).ShouldBeTrue();
    [Fact] void should_retry_driver_execution_timeouts() => _classifier.IsTransient(new MongoExecutionTimeoutException(_connection, "timeout")).ShouldBeTrue();
    [Fact] void should_retry_write_concern_errors() => _classifier.IsTransient(new MongoWriteConcernException(_connection, "write concern", new WriteConcernResult(new BsonDocument("ok", 1)))).ShouldBeTrue();
    [Fact] void should_retry_client_bulk_write_concern_errors() => _classifier.IsTransient(new ClientBulkWriteException(_connection, "bulk write concern", new Dictionary<int, WriteError>(), null, [new MongoWriteConcernException(_connection, "write concern", new WriteConcernResult(new BsonDocument("ok", 1)))], null)).ShouldBeTrue();
    [Fact] void should_retry_transient_transaction_errors() => _classifier.IsTransient(Labelled("TransientTransactionError")).ShouldBeTrue();
    [Fact] void should_retry_unknown_commit_results() => _classifier.IsTransient(Labelled("UnknownTransactionCommitResult")).ShouldBeTrue();
    [Fact] void should_retry_primary_failover() => _classifier.IsTransient(new MongoNotPrimaryException(_connection, new BsonDocument(), new BsonDocument())).ShouldBeTrue();
    [Fact] void should_retry_recovering_nodes() => _classifier.IsTransient(new MongoNodeIsRecoveringException(_connection, new BsonDocument(), new BsonDocument())).ShouldBeTrue();
    [Fact] void should_retry_lost_cursors() => _classifier.IsTransient(new MongoCursorNotFoundException(_connection, 1, new BsonDocument())).ShouldBeTrue();
    [Fact] void should_retry_labelled_writes() => _classifier.IsTransient(Labelled("RetryableWriteError")).ShouldBeTrue();
    [Fact] void should_retry_labelled_reads() => _classifier.IsTransient(Labelled("RetryableReadError")).ShouldBeTrue();
    [Fact] void should_retry_a_write_concern_without_a_write_error() => _classifier.IsTransient(WriteFailure(false)).ShouldBeTrue();
    [Fact] void should_not_hide_a_deterministic_write_error_behind_write_concern() => _classifier.IsTransient(WriteFailure(true)).ShouldBeFalse();
    [Fact] void should_not_retry_unlabelled_driver_errors() => _classifier.IsTransient(new MongoException("deterministic")).ShouldBeFalse();
    [Fact] void should_not_retry_deterministic_errors() => _classifier.IsTransient(new FormatException()).ShouldBeFalse();

    MongoWriteException WriteFailure(bool deterministic)
    {
        // The driver exposes these values on exceptions but keeps their constructors internal.
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var concern = (WriteConcernError)Activator.CreateInstance(typeof(WriteConcernError), flags, null, [64, "WriteConcernFailed", "concern", new BsonDocument(), Array.Empty<string>()], null)!;
        var write = deterministic ? (WriteError)Activator.CreateInstance(typeof(WriteError), flags, null, [ServerErrorCategory.DuplicateKey, 11000, "duplicate", new BsonDocument()], null)! : null;
        return new(_connection, write, concern, null);
    }

    static MongoException Labelled(string label)
    {
        var error = new MongoException("labelled");
        error.AddErrorLabel(label);
        return error;
    }
}
