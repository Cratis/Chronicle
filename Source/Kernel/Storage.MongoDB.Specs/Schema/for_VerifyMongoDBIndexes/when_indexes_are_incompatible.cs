// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes;

public class when_indexes_are_incompatible : Specification
{
    RecordingLogger<VerifyMongoDBIndexes> _logger;
    VerifyMongoDBIndexes _task;
    Exception? _error;
    int _inspections;

    void Establish()
    {
        _logger = new();
        _task = new(
            Options.Create(new ChronicleOptions { Storage = new() { SchemaMode = SchemaMode.Verify } }),
            _ =>
            {
                _inspections++;
                return Task.FromResult(new MongoDBIndexReport(
                [
                    new("namespace:System/Default", "System+es+Default", [], [])
                    {
                        Incompatible = ["event-log/publication_identity"]
                    }
                ]));
            },
            _logger);
    }

    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact]
    void should_warn_about_the_incompatible_index() => _logger.Entries.Any(entry =>
        entry.Level == LogLevel.Warning &&
        entry.Message.Contains("event-log/publication_identity", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_call_the_restricted_inspection_once() => _inspections.ShouldEqual(1);
}
