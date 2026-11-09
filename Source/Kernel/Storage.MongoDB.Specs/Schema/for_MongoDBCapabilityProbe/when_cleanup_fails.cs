// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_cleanup_fails : given.a_driver
{
    void Establish()
    {
        _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(given.command_errors.Create(2, "createIndex.collation is not implemented yet")));
        _mongo.DropCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("mongodb://user:secret@private-host")));
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_preserve_the_unsupported_result() => _report.Collation.State.ShouldEqual(MongoDBCapabilityState.Unsupported);
    [Fact] void should_warn() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
    [Fact] void should_not_log_connection_details() => _logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log").All(call => !call.GetArguments()[2]!.ToString()!.Contains("secret", StringComparison.Ordinal)).ShouldBeTrue();
}
