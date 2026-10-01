// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

/// <summary>
/// A write concern error leaves the outcome of every operation in the write unknown, so every partition in it is
/// reported as failed. The server's error is logged once and travels with each of those partitions.
/// </summary>
public class and_the_server_reports_a_write_concern_error : given.an_ordered_bulk_write
{
    for_Sink.given.RecordingLogger.LogEntry _entry;

    void Establish() => _writeConcernFailure = true;

    async Task Because()
    {
        await Flush();
        _entry = _logger.Entries.Single(_ => _.Values.ContainsKey("CodeName"));
    }

    [Fact] void should_log_it_as_an_error() => _entry.Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_log_the_error_code() => _entry.Values["Code"].ShouldEqual(64);
    [Fact] void should_log_the_error_code_name() => _entry.Values["CodeName"].ShouldEqual("WriteConcernFailed");
    [Fact] void should_log_the_server_message() => _entry.Values["ErrorMessage"].ShouldEqual(WriteConcernErrorMessage);
    [Fact] void should_log_how_many_operations_have_an_unknown_outcome() => _entry.Values["OperationCount"].ShouldEqual(OperationCount);
    [Fact] void should_not_log_the_document() => _logger.Entries.Any(_ => _.Message.Contains(DocumentContent)).ShouldBeFalse();
    [Fact] void should_carry_the_server_message_with_every_failed_partition() => _failedPartitions.All(_ => _.Reason.Contains(WriteConcernErrorMessage)).ShouldBeTrue();
}
