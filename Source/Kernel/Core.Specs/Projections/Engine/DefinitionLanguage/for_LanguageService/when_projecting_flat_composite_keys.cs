// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

[PerFactSpecificationLifecycle]
public class when_projecting_flat_composite_keys : given.a_runtime_language_service
{
    const string Body = """
        no automap
        from Changed
          key
            orderNumber = name
            customerId = $eventSourceId
          amount = age
        remove with Removed
          key
            customerId = $eventSourceId
            orderNumber = name
        """;

    InMemorySink _sink;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{"customerId":{"type":"string"},"orderNumber":{"type":"string"},"amount":{"type":"integer","format":"int32"}}}
            """);
        _schemas = [.. _schemas, new EventTypeSchema((EventType)"Removed", EventTypeOwner.Client, EventTypeSource.Code, _schemas[0].Schema)];
        _sink = new InMemorySink(_readModel, new TypeFormats());
    }

    async Task Because() => await Compile(Body);

    void Destroy() => _sink.Dispose();

    [Fact] void should_round_trip_both_handlers_without_inventing_a_key_type() => _generated.ShouldContain("remove with Removed\n        key\n");

    [Fact]
    public async Task should_update_the_same_tuple_without_an_id_property()
    {
        var key = await Apply("Changed", "one", 10, 1);
        await Apply("Changed", "one", 20, 2);
        var state = (IDictionary<string, object?>)(await _sink.FindOrDefault(key))!;
        state["customerId"].ShouldEqual("customer");
        state["orderNumber"].ShouldEqual("one");
        state["amount"].ShouldEqual(20);
        state.ContainsKey("id").ShouldBeFalse();
        _sink.Collection.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_remove_only_the_exact_tuple_and_ignore_a_missing_tuple()
    {
        var first = await Apply("Changed", "one", 10, 1);
        var second = await Apply("Changed", "two", 20, 2);
        await Apply("Removed", "missing", 0, 3);
        await Apply("Removed", "one", 0, 4);
        (await _sink.FindOrDefault(first)).ShouldBeNull();
        (await _sink.FindOrDefault(second)).ShouldNotBeNull();
        _sink.Collection.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_not_let_a_mapping_change_a_key_part()
    {
        await Compile(Body.Replace("amount = age", "customerId = name"));
        var exception = await Catch.Exception(() => Apply("Changed", "one", 10, 1));
        exception.ShouldBeOfExactType<FlatCompositeKeyIdentityViolated>();
        _sink.Collection.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_a_null_part_before_writing()
    {
        var exception = await Catch.Exception(() => Apply("Changed", null, 10, 1));
        exception.ShouldNotBeNull();
        _sink.Collection.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("key\n      customerId = name")]
    [InlineData("key\n      customerId = name\n      customerId = name")]
    [InlineData("key\n      id = name\n      customerId = name")]
    [InlineData("key\n      _private = name\n      customerId = name")]
    [InlineData("key\n      missing = name\n      customerId = name")]
    [InlineData("key\n      customerId.nested = name\n      orderNumber = name")]
    public void should_reject_invalid_parts(string key)
    {
        var result = _language.Compile($"projection Alerts => Alert\n  from Changed\n    {key}\n", ProjectionOwner.Client, [_readModel], _schemas);
        result.Match(_ => false, errors => errors.HasErrors).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_a_remove_handler_without_the_parts()
    {
        var result = _language.Compile("projection Alerts => Alert\n  " + Body.Replace("\n", "\n  ").Replace("remove with Removed\n    key\n      customerId = $eventSourceId\n      orderNumber = name", "remove with Removed"), ProjectionOwner.Client, [_readModel], _schemas);
        result.Match(_ => false, errors => errors.HasErrors).ShouldBeTrue();
    }

    [Fact]
    public async Task should_recheck_namespace_sink_support_when_activating_a_shared_plan()
    {
        var plan = new ProjectionPlan(
            _eventStore,
            _definition,
            _readModel,
            ProjectionFactory.DefinitionFingerprintFor(_definition),
            ProjectionFactory.ReadModelFingerprintFor(_readModel),
            _schemas);
        var supported = await _factory.Create(_eventStore, _namespace, plan);
        supported.Identifier.ShouldEqual(_definition.Identifier);
        var exception = await Catch.Exception(() => _factory.Create(_eventStore, "unsupported-namespace", plan));
        exception.ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
    }

    [Fact]
    public async Task should_refuse_an_invalid_flat_definition_before_caching_its_plan()
    {
        var exception = await Catch.Exception(() => _factory.GetPlan(_eventStore, _definition with { SubscribesToAllEvents = true }, _readModel));
        exception.ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
        _factory.CachedPlanCount.ShouldEqual(0);
    }

    async Task<Key> Apply(string eventType, string? number, int amount, ulong sequence)
    {
        var incoming = new AppendedEvent(EventContext.EmptyWithEventSourceId("customer") with { EventType = (EventType)eventType, SequenceNumber = sequence }, new { name = number, age = amount }.AsExpandoObject());
        var resolved = await _projection.GetKeyResolverFor(incoming.Context.EventType)(_eventSequenceStorage, _sink, incoming);
        var key = ((ResolvedKey)resolved).Key;
        key.ShouldBeOfExactType<FlatCompositeKey>();
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        var context = new ProjectionEventContext(key, incoming, changes, _projection.GetOperationTypeFor(incoming.Context.EventType), false);
        context = await new SetInitialState(_sink, NullLogger<SetInitialState>.Instance).Perform(_projection, context);
        await new HandleEvent(_eventSequenceStorage, _sink, NullLogger<HandleEvent>.Instance).Perform(_projection, context);
        FlatCompositeKeyState.Verify(context.Changeset, (FlatCompositeKey)key);
        await _sink.ApplyChanges(key, context.Changeset, sequence);
        return key;
    }
}
