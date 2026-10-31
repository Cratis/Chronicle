// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_TestingEventTypes;

public class when_seeding_many_event_types : Specification
{
    readonly List<KernelEvents.EventTypeToRegister[]> _batches = [];
    IEventTypesStorage _storage;
    IEventTypes _events;
    IJsonSchemaGenerator _schemas;
    IEventTypeMigrators _migrators;

    void Establish()
    {
        _storage = Substitute.For<IEventTypesStorage>();
        _storage.Register(Arg.Any<IEnumerable<KernelEvents.EventTypeToRegister>>()).Returns(call =>
        {
            _batches.Add(call.Arg<IEnumerable<KernelEvents.EventTypeToRegister>>().ToArray());
            return Task.FromResult<IEnumerable<KernelEvents.EventTypeId>>([]);
        });
        _events = Substitute.For<IEventTypes>();
        _events.All.Returns(Enumerable.Range(0, 100).Select(index => new EventType($"event-{index}", 1)).Append(new EventType("event-0", 2, true)).ToImmutableList());
        _events.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(Recorded));
        _schemas = Substitute.For<IJsonSchemaGenerator>();
        _schemas.Generate(typeof(Recorded), _events).Returns(_ => new JsonSchema());
        _migrators = Substitute.For<IEventTypeMigrators>();
        _migrators.GetMigratorsFor(typeof(Recorded)).Returns([]);
    }

    Task Because() => TestingEventTypes.Seed(_storage, _events, _schemas, _migrators);

    [Fact] void should_register_in_one_batch_regardless_of_catalog_size() => _batches.Count.ShouldEqual(1);
    [Fact] void should_not_register_types_individually() => _storage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventTypesStorage.Register)).ShouldEqual(1);
    [Fact] void should_include_every_event_in_each_batch() => _batches.TrueForAll(batch => batch.Length == 100).ShouldBeTrue();
    [Fact] void should_preserve_all_generations() => _batches[0].Single(item => item.Definition.Id.Value == "event-0").Definition.Generations.Count().ShouldEqual(2);
    [Fact] void should_use_the_latest_tombstone() => _batches[0].Single(item => item.Definition.Id.Value == "event-0").Definition.Tombstone.ShouldBeTrue();
    [Fact] void should_preserve_code_sources() => _batches.SelectMany(batch => batch).All(item => item.Source == KernelEvents.EventTypeSource.Code).ShouldBeTrue();
    [Fact] void should_keep_unspecified_visibility_for_a_custom_registry() => _batches[0].All(item => item.Visibility == KernelEvents.EventTypeVisibility.Unspecified).ShouldBeTrue();

    public record Recorded;
}
