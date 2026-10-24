// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_namespace_reports_more_rows_than_the_limit : given.a_statistics_reader
{
    Exception _error;

    void Establish() => _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(new GetInstancesResponse
    {
        Instances = [],
        TotalCount = StatisticsReader.MaximumRows + 1
    });

    async Task Because() => _error = await Catch.Exception(() => _reader.GetEventTypeStatistics("store", "overflow"));

    [Fact] void should_fail_even_if_the_returned_page_is_short() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
