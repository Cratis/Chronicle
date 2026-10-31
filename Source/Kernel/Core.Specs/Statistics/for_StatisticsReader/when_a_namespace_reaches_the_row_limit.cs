// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_a_namespace_reaches_the_row_limit : given.a_statistics_reader
{
    ILogger<StatisticsReader> _logger;
    Exception _error;
    readonly List<KeyValuePair<string, object?>[]> _warnings = [];

    void Establish()
    {
        _logger = Substitute.For<ILogger<StatisticsReader>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object)!, default, default!))
            .Do(call =>
            {
                if (call.ArgAt<LogLevel>(0) == LogLevel.Warning)
                {
                    _warnings.Add(call.ArgAt<IEnumerable<KeyValuePair<string, object?>>>(2).ToArray());
                }
            });
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<Namespaces.INamespaces>("store").Returns(_namespaces);
        _reader = new(_readModels, factory, _logger);
        _namespaces.GetAll().Returns(["capped"]);
        _readModels.GetInstances(Arg.Any<GetInstancesRequest>(), Arg.Any<CallContext>()).Returns(new GetInstancesResponse
        {
            Instances = Enumerable.Repeat("{\"eventType\":\"event\",\"count\":1}", StatisticsReader.MaximumRows).ToList()
        });
    }

    async Task Because() => _error = await Catch.Exception(() => _reader.GetEventTypeStatisticsForEventStore("store"));

    [Fact] void should_fail_rather_than_return_partial_totals() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_identify_the_capped_namespace() => _error.Message.ShouldContain("capped");
    [Fact] void should_log_a_structured_warning()
    {
        var values = _warnings.Single().ToDictionary(pair => pair.Key, pair => pair.Value);
        values["EventStore"]!.ToString().ShouldEqual("store");
        values["Namespace"]!.ToString().ShouldEqual("capped");
        values["MaximumRows"].ShouldEqual(StatisticsReader.MaximumRows);
        values["ReturnedRows"].ShouldEqual(StatisticsReader.MaximumRows);
    }
}
