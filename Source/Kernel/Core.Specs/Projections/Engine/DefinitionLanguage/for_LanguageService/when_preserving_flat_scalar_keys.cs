// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

[PerFactSpecificationLifecycle]
public class when_preserving_flat_scalar_keys : given.a_runtime_language_service
{
    const string Body = """
        from Changed
          key
            label = name
            number = age
            enabled = active
            correlation = id
        """;

    readonly Guid _correlation = Guid.Parse("11111111-1111-4111-8111-111111111111");
    InMemorySink _sink;
    FlatCompositeKey _key;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{"label":{"type":"string"},"number":{"type":"integer","format":"int32"},"enabled":{"type":"boolean"},"correlation":{"type":"string","format":"uuid"}}}
            """);
        _schemas = [new EventTypeSchema((EventType)"Changed", EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("""
            {"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer","format":"int64"},"active":{"type":"boolean"},"id":{"type":"string","format":"uuid"},"number":{"type":"integer","format":"int32"}}}
            """))];
        _sink = new InMemorySink(_readModel, new TypeFormats());
    }

    async Task Because()
    {
        await Compile(Body);
        _key = await Apply(1);
    }

    void Destroy() => _sink.Dispose();

    [Fact]
    async Task should_persist_all_scalar_parts_without_ordinary_mappings()
    {
        var state = (IDictionary<string, object?>)(await _sink.FindOrDefault(_key))!;
        state["label"].ShouldEqual("order");
        state["number"].ShouldBeOfExactType<int>();
        state["number"].ShouldEqual(42);
        state["enabled"].ShouldEqual(true);
        state["correlation"].ShouldBeOfExactType<Guid>();
        state["correlation"].ShouldEqual(_correlation);
        state.ContainsKey("id").ShouldBeFalse();
        state.ContainsKey("_id").ShouldBeFalse();
    }

    [Fact]
    async Task should_find_the_same_instance_with_reordered_detached_parts()
    {
        var parts = new ExpandoObject();
        var values = (IDictionary<string, object?>)parts;
        foreach (var part in _key.Parts.Reverse()) values[part.Key] = part.Value;
        (await _sink.FindOrDefault(new FlatCompositeKey(parts, ArrayIndexers.NoIndexers))).ShouldNotBeNull();
        _sink.Collection.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_allow_an_equal_redundant_assignment()
    {
        await Compile(Body + "\n  number = age");
        await Apply(2);
        _sink.Collection.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_refuse_arithmetic_that_changes_identity_without_mutating_the_cached_instance()
    {
        await Compile(Body + "\n  increment number");
        (await Catch.Exception(() => Apply(2))).ShouldBeOfExactType<FlatCompositeKeyIdentityViolated>();
        ((IDictionary<string, object?>)(await _sink.FindOrDefault(_key))!)["number"].ShouldEqual(42);
        _sink.Collection.Count.ShouldEqual(1);
    }

    [Fact]
    async Task should_refuse_an_automapped_conflicting_value_without_mutating_the_cached_instance()
    {
        var definition = _definition with { AutoMap = AutoMap.Enabled };
        _projection = await _factory.Create(_eventStore, _namespace, definition, _readModel, _schemas);
        (await Catch.Exception(() => Apply(2))).ShouldBeOfExactType<FlatCompositeKeyIdentityViolated>();
        ((IDictionary<string, object?>)(await _sink.FindOrDefault(_key))!)["number"].ShouldEqual(42);
    }

    async Task<FlatCompositeKey> Apply(ulong sequence)
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("customer") with { EventType = (EventType)"Changed", SequenceNumber = sequence },
            new { name = "order", age = 42L, active = true, id = _correlation.ToString("D"), number = 99 }.AsExpandoObject());
        var resolved = await _projection.GetKeyResolverFor(incoming.Context.EventType)(_eventSequenceStorage, _sink, incoming);
        var key = (FlatCompositeKey)((ResolvedKey)resolved).Key;
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        var context = new ProjectionEventContext(key, incoming, changes, _projection.GetOperationTypeFor(incoming.Context.EventType), false);
        context = await new SetInitialState(_sink, NullLogger<SetInitialState>.Instance).Perform(_projection, context);
        context = await new HandleEvent(_eventSequenceStorage, _sink, NullLogger<HandleEvent>.Instance).Perform(_projection, context);
        await new VerifyFlatCompositeKey().Perform(_projection, context);
        await _sink.ApplyChanges(key, context.Changeset, sequence);
        return key;
    }
}
