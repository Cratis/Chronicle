// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_flat_identity_is_violated_in_a_pipeline : given.a_runtime_language_service
{
    ISink _sink;
    IChangesetStorage _history;
    IProjectionFutures _futures;
    IReadModelsCompliance _compliance;
    ExpandoObject _cached;
    Exception _exception;

    async Task Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"customerId":{"type":"string"},"orderNumber":{"type":"string"}}}
            """);
        await Compile("""
            from Changed
              key
                customerId = $eventSourceId
                orderNumber = name
              customerId = name
            """);
        _cached = new { customerId = "customer", orderNumber = "order" }.AsExpandoObject();
        _sink = Substitute.For<ISink>();
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_ => _cached);
        _history = Substitute.For<IChangesetStorage>();
        _futures = Substitute.For<IProjectionFutures>();
        _futures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([]));
        _compliance = Substitute.For<IReadModelsCompliance>();
    }

    async Task Because()
    {
        var storage = Substitute.For<IStorage>();
        var namespaceStorage = storage.GetEventStore(_eventStore).GetNamespace(_namespace);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);
        namespaceStorage.Sinks.GetFor(_readModel).Returns(_sink);
        namespaceStorage.Changesets.Returns(_history);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IProjectionFutures>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_futures);
        var manager = new ProjectionPipelineManager(
            storage,
            grainFactory,
            new ObjectComparer(),
            new TypeFormats(),
            _compliance,
            Options.Create(new Configuration.ChronicleOptions()),
            NullLoggerFactory.Instance,
            Substitute.For<IReadModelSinkProvenance>());
        var pipeline = await manager.GetFor(_eventStore, _namespace, _projection);
        _sink.ClearReceivedCalls();
        _history.ClearReceivedCalls();
        _futures.ClearReceivedCalls();
        _compliance.ClearReceivedCalls();
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("customer") with { EventType = (EventType)"Changed", SequenceNumber = 1 },
            new { name = "order", age = 1 }.AsExpandoObject());
        _exception = await Catch.Exception(() => pipeline.Handle(incoming));
    }

    [Fact] void should_report_an_explicit_projection_failure() => _exception.ShouldBeOfExactType<FlatCompositeKeyIdentityViolated>();

    [Fact]
    void should_only_read_the_sink()
    {
        var calls = _sink.ReceivedCalls().ToArray();
        calls.Length.ShouldEqual(2);
        calls[0].GetMethodInfo().Name.ShouldEqual(nameof(ISink.EnsureProvenance));
        calls[1].GetMethodInfo().Name.ShouldEqual(nameof(ISink.FindOrDefault));
    }

    [Fact] void should_not_write_history_or_success_notifications() => _history.ReceivedCalls().ShouldBeEmpty();

    [Fact] void should_not_store_or_resolve_futures() => _futures.ReceivedCalls().ShouldBeEmpty();

    [Fact] void should_fail_before_encrypting_changes() => _compliance.ReceivedCalls().ShouldBeEmpty();

    [Fact]
    void should_not_mutate_or_initialize_the_cached_state()
    {
        var values = (IDictionary<string, object?>)_cached;
        values["customerId"].ShouldEqual("customer");
        values["orderNumber"].ShouldEqual("order");
        values.Count.ShouldEqual(2);
    }
}
