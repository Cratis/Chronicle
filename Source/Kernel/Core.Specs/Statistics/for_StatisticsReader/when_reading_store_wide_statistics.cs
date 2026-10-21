// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_reading_store_wide_statistics : given.a_statistics_reader
{
    IEnumerable<EventTypeStatistics> _result;

    void Establish()
    {
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            var request = call.Arg<GetInstancesRequest>();
            IList<string> instances = request.Namespace switch
            {
                "first" => ["{\"eventType\":\"shared\",\"namespace\":\"first\",\"count\":3}"],
                "second" => ["{\"eventType\":\"shared\",\"namespace\":\"second\",\"count\":5}"],
                _ => []
            };
            return new GetInstancesResponse { Instances = instances };
        });
        _namespaces.GetAll().Returns(["first", "second"]);
    }

    async Task Because() => _result = await _reader.GetEventTypeStatisticsForEventStore("store");

    [Fact] void should_concatenate_the_namespaced_rows() => _result.Select(row => row.Namespace).ShouldContainOnly("first", "second");
    [Fact] void should_preserve_each_count() => _result.Select(row => row.Count).ShouldContainOnly(3, 5);
    [Fact] async Task should_apply_the_row_limit_per_namespace() => await _readModels.Received(2).GetInstances(
        Arg.Is<GetInstancesRequest>(request => request.EventStore == "store" && request.Page == 0 && request.PageSize == StatisticsReader.MaximumRows), Arg.Any<CallContext>());
    [Fact] async Task should_not_read_the_shared_container() => await _readModels.DidNotReceive().GetInstances(
        Arg.Is<GetInstancesRequest>(request => request.Namespace == Concepts.EventStoreNamespaceName.NotSet.Value), Arg.Any<CallContext>());
}
