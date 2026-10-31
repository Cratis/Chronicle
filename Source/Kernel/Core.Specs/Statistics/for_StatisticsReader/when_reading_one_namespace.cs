// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_reading_one_namespace : given.a_statistics_reader
{
    IEnumerable<EventTypeStatistics> _result;

    void Establish() => _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(
        new GetInstancesResponse { Instances = ["{\"eventType\":\"shared\",\"namespace\":\"first\",\"count\":3}"] });

    async Task Because() => _result = await _reader.GetEventTypeStatistics("store", "first");

    [Fact] void should_return_the_namespace_rows() => _result.Single().Namespace.ShouldEqual("first");
    [Fact] async Task should_not_enumerate_other_namespaces() => await _namespaces.DidNotReceive().GetAll();
    [Fact] async Task should_read_only_that_container() => await _readModels.Received(1).GetInstances(
        Arg.Is<GetInstancesRequest>(request => request.Namespace == "first"), Arg.Any<CallContext>());
}
