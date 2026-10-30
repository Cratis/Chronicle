// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_the_namespace_membership_changes_during_a_read : given.a_statistics_reader
{
    readonly List<EventStoreNamespaceName> _membership = ["first", "removed"];
    IEnumerable<EventTypeStatistics> _result;

    void Establish()
    {
        _namespaces.GetAll().Returns(_membership);
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            lock (_membership)
            {
                _membership.Clear();
                _membership.Add("added");
            }
            var name = call.Arg<GetInstancesRequest>().Namespace;
            return new GetInstancesResponse { Instances = [$"{{\"namespace\":\"{name}\",\"eventType\":\"event\",\"count\":1}}"] };
        });
    }

    async Task Because() => _result = await _reader.GetEventTypeStatisticsForEventStore("store");

    [Fact] void should_include_only_the_initial_membership() => _result.Select(row => row.Namespace).ShouldContainOnly("first", "removed");
    [Fact] async Task should_discover_membership_once() => await _namespaces.Received(1).GetAll();
    [Fact] async Task should_not_read_a_later_addition() => await _readModels.DidNotReceive().GetInstances(
        Arg.Is<GetInstancesRequest>(request => request.Namespace == "added"), Arg.Any<CallContext>());
}
