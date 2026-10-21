// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately.given;

public class a_faulted_threshold_sink(MongoDBFixture fixture) : a_controlled_mongo_sink(fixture)
{
    protected FailedPartition[] _thresholdFailures = [];

    async Task Establish()
    {
        _firstBatchError = _unavailable;
        await QueueBelowThreshold();
        _thresholdFailures = (await ReachThreshold()).ToArray();
    }
}
