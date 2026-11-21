// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_system_statistics_are_read_without_a_request : given.an_http_statistics_reader
{
    protected override bool HasAmbientRequest => false;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_before_reading_statistics(bool storeWide)
    {
        BeginRequest();
        var exception = await Catch.Exception(async () =>
        {
            if (storeWide) await EventStatistics.StatisticsForEventStore(_reader, "System");
            else await EventStatistics.StatisticsForNamespace(_reader, "System", "first");
        });

        ((RpcException)exception!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        _sink.ReceivedCalls().ShouldBeEmpty();
        _secondSink.ReceivedCalls().ShouldBeEmpty();
    }
}
