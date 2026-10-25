// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes;

public class when_indexes_are_missing : Specification
{
    ILogger<VerifyMongoDBIndexes> _logger;
    VerifyMongoDBIndexes _task;
    Exception? _error;
    int _inspections;

    void Establish()
    {
        _logger = Substitute.For<ILogger<VerifyMongoDBIndexes>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _task = new(
            Options.Create(new ChronicleOptions { Storage = new() { SchemaMode = SchemaMode.Verify } }),
            _ =>
            {
                _inspections++;
                return Task.FromResult(new MongoDBIndexReport([new("namespace:System/Default", "System+es+Default", ["event-log/subject_index"], [])]));
            },
            _logger);
    }

    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] void should_warn_about_the_missing_index() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
    [Fact] void should_call_the_restricted_inspection_once() => _inspections.ShouldEqual(1);
}
