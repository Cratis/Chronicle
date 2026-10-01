// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

/// <summary>
/// A failed partition used to be recorded as nothing more than "Bulk operation failed for partition X", with the
/// server's error discarded. The error is now logged and travels with the failed partition - its code and the
/// server's message, never the document being written.
/// </summary>
public class and_the_server_reports_a_write_error : given.an_ordered_bulk_write
{
    for_Sink.given.RecordingLogger.LogEntry _entry;

    void Establish() => _failureIndexes = [500];

    async Task Because()
    {
        await Flush();
        _entry = _logger.Entries.Single(_ => _.Values.ContainsKey("Code"));
    }

    [Fact] void should_log_it_as_an_error() => _entry.Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_log_the_error_code() => _entry.Values["Code"].ShouldEqual(11000);
    [Fact] void should_log_the_error_category() => _entry.Values["Category"].ShouldEqual(ServerErrorCategory.DuplicateKey);
    [Fact] void should_log_the_server_message() => _entry.Values["ErrorMessage"].ShouldEqual(WriteErrorMessage);
    [Fact] void should_log_the_index_of_the_failed_operation() => _entry.Values["OperationIndex"].ShouldEqual(500);
    [Fact] void should_log_the_partition() => _entry.Values["Partition"].ShouldEqual("key-500");
    [Fact] void should_not_attach_the_exception() => _entry.HasException.ShouldBeFalse();
    [Fact] void should_not_log_the_document() => _logger.Entries.Any(_ => _.Message.Contains(DocumentContent)).ShouldBeFalse();
    [Fact] void should_carry_the_error_code_with_the_failed_partition() => _failedPartitions.Single().Reason.ShouldContain("11000");
    [Fact] void should_carry_the_server_message_with_the_failed_partition() => _failedPartitions.Single().Reason.ShouldContain(WriteErrorMessage);
    [Fact] void should_not_carry_the_document_with_the_failed_partition() => _failedPartitions.Single().Reason.ShouldNotContain(DocumentContent);
}
