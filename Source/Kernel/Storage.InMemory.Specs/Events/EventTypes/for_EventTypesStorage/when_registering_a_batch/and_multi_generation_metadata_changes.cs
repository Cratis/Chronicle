// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_registering_a_batch;

public class and_multi_generation_metadata_changes : given.a_batch_of_event_types
{
    EventTypeSchema _schema;
    IDisposable _subscription;

    async Task Establish()
    {
        await _storage.Register(_batch);
        _subscription = _storage.ObserveLatestForAllEventTypes().Skip(1).Subscribe(_published.Add);
    }

    async Task Because()
    {
        await _storage.Register([_multiGeneration with { Source = EventTypeSource.User, Visibility = EventTypeVisibility.Public, Origin = "foreign" }]);
        await _storage.Register([_multiGeneration with { Source = EventTypeSource.User, Origin = "foreign" }]);
        _schema = await _storage.GetFor(new EventTypeId("migrated-event"));
    }

    [Fact] void should_preserve_declared_visibility_when_the_next_batch_says_nothing() => _schema.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_record_the_source() => _schema.Source.ShouldEqual(EventTypeSource.User);
    [Fact] void should_record_the_origin() => _schema.Origin.ShouldEqual("foreign");
    [Fact] void should_publish_the_metadata_change_once() => _published.Count.ShouldEqual(1);
    [Fact] async Task should_keep_both_generations() => (await _storage.GetDefinition(_multiGeneration.Definition.Id)).Generations.Count().ShouldEqual(2);
    [Fact] async Task should_keep_migrations() => (await _storage.GetDefinition(_multiGeneration.Definition.Id)).Migrations.Count().ShouldEqual(1);

    void Destroy() => _subscription?.Dispose();
}
