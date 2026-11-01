// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes;

public class when_verification_fails : Specification
{
    ILogger<VerifyMongoDBIndexes> _logger;
    VerifyMongoDBIndexes _task;
    Exception? _error;

    void Establish()
    {
        _logger = Substitute.For<ILogger<VerifyMongoDBIndexes>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _task = new(
            Options.Create(new ChronicleOptions { Storage = new() { SchemaMode = SchemaMode.Verify } }),
            _ => Task.FromException<MongoDBIndexReport>(new IOException("verification unavailable")),
            _logger);
    }

    async Task Because() => _error = await Catch.Exception(() => _task.Execute(CancellationToken.None));

    [Fact] void should_continue_startup() => _error.ShouldBeNull();
    [Fact] void should_warn() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
}
