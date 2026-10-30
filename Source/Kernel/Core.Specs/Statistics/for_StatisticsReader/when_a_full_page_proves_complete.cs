// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_full_page_proves_complete : given.a_statistics_reader
{
    IEnumerable<EventTypeStatistics> _result;

    void Establish()
    {
        _namespaces.GetAll().Returns(["complete"]);
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(new GetInstancesResponse
        {
            Instances = Enumerable.Repeat("{\"eventType\":\"event\",\"count\":1}", StatisticsReader.MaximumRows).ToList(),
            TotalCount = StatisticsReader.MaximumRows
        });
    }

    async Task Because() => _result = await _reader.GetEventTypeStatisticsForEventStore("store");

    [Fact] void should_return_the_complete_page() => _result.Count().ShouldEqual(StatisticsReader.MaximumRows);
    [Fact] void should_preserve_the_totals() => _result.Sum(row => row.Count).ShouldEqual(StatisticsReader.MaximumRows);
}
