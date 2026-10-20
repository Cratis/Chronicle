// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_the_store_has_no_namespaces : given.a_statistics_reader
{
    IEnumerable<EventTypeStatistics> _result;

    async Task Because() => _result = await _reader.GetEventTypeStatisticsForEventStore("store");

    [Fact] void should_return_no_rows() => _result.ShouldBeEmpty();
    [Fact] async Task should_not_read_a_container() => await _readModels.DidNotReceive().GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>());
}
