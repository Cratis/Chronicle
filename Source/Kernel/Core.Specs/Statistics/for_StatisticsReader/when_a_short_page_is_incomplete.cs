// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_short_page_is_incomplete : given.a_statistics_reader
{
    Exception _error;

    void Establish() => _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(new GetInstancesResponse
    {
        Instances = ["{\"eventType\":\"event\",\"count\":1}"],
        TotalCount = 2
    });

    async Task Because() => _error = await Catch.Exception(() => _reader.GetEventTypeStatistics("store", "incomplete"));

    [Fact] void should_fail_rather_than_return_a_partial_total() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
