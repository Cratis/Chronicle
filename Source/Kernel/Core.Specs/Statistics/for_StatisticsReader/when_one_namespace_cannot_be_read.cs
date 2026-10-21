// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_one_namespace_cannot_be_read : given.a_statistics_reader
{
    readonly Exception _failure = new InvalidOperationException("Namespace read failed");
    Exception _error;

    void Establish()
    {
        _namespaces.GetAll().Returns(["first", "second"]);
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(call =>
            call.Arg<GetInstancesRequest>().Namespace == "first"
                ? Task.FromResult(new GetInstancesResponse { Instances = ["{\"eventType\":\"shared\",\"namespace\":\"first\",\"count\":3}"] })
                : Task.FromException<GetInstancesResponse>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => _reader.GetEventTypeStatisticsForEventStore("store"));

    [Fact] void should_not_report_partial_totals() => _error.ShouldEqual(_failure);
}
