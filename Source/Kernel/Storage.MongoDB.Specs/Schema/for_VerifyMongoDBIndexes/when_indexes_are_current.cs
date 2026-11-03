// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes;

public class when_indexes_are_current : Specification
{
    ILogger<VerifyMongoDBIndexes> _logger;
    VerifyMongoDBIndexes _task;

    void Establish()
    {
        _logger = Substitute.For<ILogger<VerifyMongoDBIndexes>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _task = new(
            Options.Create(new ChronicleOptions { Storage = new() { SchemaMode = SchemaMode.Verify } }),
            _ => Task.FromResult(new MongoDBIndexReport([new("namespace:System/Default", "System+es+Default", [], [])])),
            _logger);
    }

    Task Because() => _task.Execute(CancellationToken.None);

    [Fact] void should_not_warn() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log").ShouldBeFalse();
}
