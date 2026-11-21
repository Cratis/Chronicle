// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using StatisticsService = Cratis.Chronicle.Services.Statistics.Statistics;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_an_administrator_calls_the_statistics_service : given.an_http_statistics_reader
{
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 8)]
    public async Task should_return_the_guarded_statistics_payload(bool storeWide, long expectedTotal)
    {
        BeginRequest();
        var casing = Substitute.For<INamespaceCasing>();
        casing.Canonicalize(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call => call.ArgAt<EventStoreNamespaceName>(1));
        var service = new StatisticsService(_reader, casing, NullLogger<StatisticsService>.Instance);
        var context = new CallContext(new object(), new FakeServerCallContext(_httpContext));
        var result = storeWide
            ? await service.StatisticsForEventStore(new() { EventStore = "System" }, context)
            : await service.StatisticsForNamespace(new() { EventStore = "System", Namespace = "first" }, context);

        result.IsAuthorized.ShouldBeTrue();
        result.Data.TotalEvents.ShouldEqual(expectedTotal);
        result.Data.PerEventType.Single().EventType.ShouldEqual("shared");
    }
}
