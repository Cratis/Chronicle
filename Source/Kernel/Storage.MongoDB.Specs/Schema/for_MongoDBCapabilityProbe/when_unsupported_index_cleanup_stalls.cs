// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_unsupported_index_cleanup_stalls : given.a_driver
{
    readonly TaskCompletionSource _drop = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(given.command_errors.Create(2, "createIndex.collation is not implemented yet")));
        _mongo.DropCollectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_drop.Task);
        _probe = new(_database, Options.Create(_options), _logger, TimeSpan.FromMilliseconds(50));
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_preserve_the_unsupported_result() => _report.Collation.State.ShouldEqual(MongoDBCapabilityState.Unsupported);
    [Fact] void should_not_wait_for_the_drop_to_complete() => _drop.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_warn_about_cleanup() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldBeTrue();
}
