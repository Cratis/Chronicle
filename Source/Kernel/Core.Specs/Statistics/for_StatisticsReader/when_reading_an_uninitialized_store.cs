// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_reading_an_uninitialized_store : given.a_statistics_reader
{
    void Establish() => _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
        .Returns(new GetInstancesResponse { Instances = [], TotalCount = 0 });

    Task Because() => _reader.GetEventTypeStatistics("store", "namespace");

    [Fact] void should_initialize_before_reading() => Received.InOrder(() =>
    {
        _initialization.Ensure("store");
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
    });
}
