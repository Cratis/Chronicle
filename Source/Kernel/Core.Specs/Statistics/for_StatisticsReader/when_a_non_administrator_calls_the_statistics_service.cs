// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Services.for_QueryExecutor;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using StatisticsService = Cratis.Chronicle.Services.Statistics.Statistics;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_non_administrator_calls_the_statistics_service : Services.for_SystemStoreGuard.given.an_in_process_request
{
    IStatisticsReader _reader;
    StatisticsService _service;

    void Establish()
    {
        _reader = Substitute.For<IStatisticsReader>();
        _service = new(_reader, Substitute.For<INamespaceCasing>(), NullLogger<StatisticsService>.Instance);
    }

    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_store_statistics_before_invoking_the_reader(string caller)
    {
        _httpContext.User = CallerNamed(caller);
        BeginRequest();
        var context = new CallContext(new object(), new FakeServerCallContext(_httpContext));
        var result = await _service.StatisticsForEventStore(new() { EventStore = "System" }, context);

        result.IsAuthorized.ShouldBeFalse();
        _reader.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_namespace_statistics_before_invoking_the_reader(string caller)
    {
        _httpContext.User = CallerNamed(caller);
        BeginRequest();
        var context = new CallContext(new object(), new FakeServerCallContext(_httpContext));
        var result = await _service.StatisticsForNamespace(new() { EventStore = "System", Namespace = "first" }, context);

        result.IsAuthorized.ShouldBeFalse();
        _reader.ReceivedCalls().ShouldBeEmpty();
    }
}
