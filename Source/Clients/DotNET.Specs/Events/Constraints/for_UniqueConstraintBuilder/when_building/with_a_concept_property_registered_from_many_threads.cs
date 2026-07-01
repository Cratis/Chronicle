// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

/// <summary>
/// Reproduces the reported failure: registering a unique constraint on a <c>ConceptAs&lt;T&gt;</c> property
/// intermittently threw <see cref="PropertyDoesNotExistOnEventType"/> because the client hands the same cached
/// event-type <see cref="JsonSchema"/> to every registration, and flattening its lazy caches concurrently could
/// observe a half-built cache. Runs many registrations in parallel against one shared schema per round.
/// </summary>
public class with_a_concept_property_registered_from_many_threads : given.all_dependencies
{
    const int Rounds = 300;
    const int Threads = 16;

    EventType _eventType;
    FakeEventTypes _fakeEventTypes;
    ConcurrentBag<Exception> _failures;

    void Establish()
    {
        _eventType = new EventType(nameof(AccountRegistered), EventTypeGeneration.First);
        _fakeEventTypes = new FakeEventTypes(_eventType, typeof(AccountRegistered));
    }

    void Because()
    {
        _failures = [];

        for (var round = 0; round < Rounds; round++)
        {
            // One shared schema instance per round — the same object every registration flattens, exactly as the
            // client EventTypes cache returns it. Fresh per round so each round races on an uninitialized cache.
            _fakeEventTypes.Schema = _generator.Generate(typeof(AccountRegistered));
            using var start = new ManualResetEventSlim(false);

            var registrations = Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                start.Wait();
                try
                {
                    new UniqueConstraintBuilder(_fakeEventTypes, _namingPolicy).On<AccountRegistered>(e => e.Email);
                }
                catch (Exception ex)
                {
                    _failures.Add(ex);
                }
            })).ToArray();

            start.Set();
            Task.WaitAll(registrations);
        }
    }

    [Fact] void should_register_the_concept_constraint_on_every_thread() => _failures.ShouldBeEmpty();

    sealed class FakeEventTypes(EventType eventType, Type eventClrType) : IEventTypes
    {
        public JsonSchema Schema { get; set; } = new();

        public IImmutableList<Type> AllClrTypes => throw new NotSupportedException();
        public IImmutableList<EventType> All => throw new NotSupportedException();
        public Task Discover() => throw new NotSupportedException();
        public Task Register() => throw new NotSupportedException();
        public bool HasFor(EventTypeId eventTypeId) => throw new NotSupportedException();
        public bool HasFor(Type clrType) => throw new NotSupportedException();
        public Type GetClrTypeFor(EventTypeId eventTypeId) => eventClrType;
        public Type GetClrTypeFor(EventTypeId eventTypeId, EventTypeGeneration generation) => eventClrType;
        public JsonSchema GetSchemaFor(EventTypeId eventTypeId) => Schema;
        public EventType GetEventTypeFor(Type clrType) => eventType;
    }
}
