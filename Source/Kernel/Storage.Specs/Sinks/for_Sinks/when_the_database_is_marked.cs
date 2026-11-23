// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_the_database_is_marked : given.a_sink_factory
{
    readonly List<Exception?> _errors = [];
    bool _marked;

    void Establish()
    {
        var marker = Substitute.For<ISinkProvenanceStorage>();
        marker.IsQuarantined().Returns(_ => Task.FromResult(_marked));
        _factory.GetProvenance(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<SinkName>(), Arg.Any<ReadModelContainerName?>()).Returns(marker);
    }

    async Task Because()
    {
        await _sinks.GetFor(_model);
        _factory.ClearReceivedCalls();
        _sink.ClearReceivedCalls();
        _marked = true;
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(_model)));
        _errors.Add(await Catch.Exception(() => _sinks.GetWithoutProvisioning(_model)));
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(_model, "retained-copy")));
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(_model with { ContainerName = "new-container" })));
    }

    [Fact] void should_refuse_cached_explicit_and_new_destinations() => _errors.All(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_not_construct_a_sink() => _factory.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "CreateFor").ShouldBeFalse();
    [Fact] void should_not_provision_or_return_the_cached_sink() => _sink.ReceivedCalls().Any().ShouldBeFalse();
}
