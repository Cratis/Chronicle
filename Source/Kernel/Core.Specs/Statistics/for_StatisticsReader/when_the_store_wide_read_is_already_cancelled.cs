// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_the_store_wide_read_is_already_cancelled : given.a_statistics_reader
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _reader.GetEventTypeStatisticsForEventStore("store", new CancellationToken(true)));

    [Fact] void should_cancel_the_request() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] async Task should_not_discover_namespaces() => await _namespaces.DidNotReceive().GetAll();
    [Fact] async Task should_not_read_any_namespace() => await _readModels.DidNotReceive().GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>());
}
